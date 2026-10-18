namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class SaAmgSolver
	{
		private readonly ICoarseSystemSolver coarsestSystemSolver;
		private readonly ICycleSchedule cycleSchedule;
		private readonly SaAmgOperatorHierarchy hierarchy;
		private readonly int maxCycles;
		private readonly double residualTolerance;
		private readonly MultigridSmoothers smoothers;

		protected LevelProgression levelProgression;
		protected Vector[] vectorsLhs;
		protected Vector[] vectorsRhs;
		protected Vector[] vectorsWork;

		public SaAmgSolver(SaAmgOperatorHierarchy hierarchy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance)
		{
			Logger = new MultigridSolverLogger<SaAmgHierarchyLog>(Name);

			this.hierarchy = hierarchy;
			this.smoothers = smoothers;
			this.coarsestSystemSolver = coarsestSystemSolver;
			this.cycleSchedule = cycleSchedule;
			this.maxCycles = maxCycles;
			this.residualTolerance = residualTolerance;
		}

		public string Name { get; } = "Smooth Aggregation AMG solver";

		public MultigridSolverLogger<SaAmgHierarchyLog> Logger { get; }
		
		public SaAmgOperatorHierarchy OperatorHierarchy => hierarchy;

		public void Solve(CsrMatrix systemMatrix, Vector rhsVector, Vector solution)
		{
			var residualHistory = new ResidualNormHistory();
			PrepareHierarchyAndOperators(systemMatrix);
			PrepareSystemVectors(rhsVector);

			var watch = new Stopwatch();
			watch.Start();

			vectorsRhs[0] = rhsVector;
			vectorsLhs[0] = solution;

			var res = Vector.CreateZero(rhsVector.Length);
			double normRes0 = rhsVector.Norm2();
			residualHistory.LogInitialResidual(normRes0);
			double resRatio = 1.0;
			int cycleIdx;
			for (cycleIdx = 0; cycleIdx < maxCycles; cycleIdx++)
			{
				RunSingleCycle();

				// Calculate the residual of the coarsest grid.
				//TODO: Perhaps this is already done inside the RunSingleCycle().
				systemMatrix.MultiplyIntoResult(solution, res);
				res.LinearCombinationIntoThis(-1, rhsVector, 1);
				double normRes = res.Norm2();
				residualHistory.LogResidual(normRes);
				
				resRatio = normRes / normRes0;
				if (resRatio <= residualTolerance)
				{
					break;
				}
			}

			watch.Stop();
			Logger.LogIterativeAlgorithm(cycleIdx + 1, resRatio);
			Logger.LogResidualHistory(residualHistory);
			Logger.LogTaskDuration("Execution of cycles", watch.ElapsedMilliseconds);

			if (resRatio > residualTolerance)
			{
				throw new IterativeSolverNotConvergedException(
					$"{Name} did not converge to a solution. The solver ran for {maxCycles} cycles iterations and ||b-A*x|| / ||b|| is {resRatio}.");
			}
		}

		private void PrepareHierarchyAndOperators(CsrMatrix systemMatrix)
		{
			var watch = new Stopwatch();

			watch.Start();
			hierarchy.BuildOperators(systemMatrix);
			int numLevels = hierarchy.NumLevels;
			watch.Stop();
			Logger.LogTaskDuration("Prepare prolongations, restrictions and coarse matrices", watch.ElapsedMilliseconds);
			Logger.LogOperatorHierarchy(hierarchy.Log);

			watch.Restart();
			levelProgression = cycleSchedule.CreateProgression(hierarchy.NumLevels);
			watch.Stop();
			Logger.LogTaskDuration("Prepare level progression", watch.ElapsedMilliseconds);

			watch.Restart();
			if (smoothers.NumLevelsTotal < numLevels)
			{
				throw new Exception($"Not enough smoothers are provided for the {numLevels} actual levels.");
			}
			for (int lvl = 0; lvl < numLevels - 1; lvl++)
			{
				smoothers.Update(lvl, hierarchy.GetSystemMatrix(lvl), areDofsModified: true);
			}
			watch.Stop();
			Logger.LogTaskDuration("Prepare smoothers", watch.ElapsedMilliseconds);

			watch.Restart();
			coarsestSystemSolver.Initialize(hierarchy.GetSystemMatrix(numLevels - 1));
			watch.Stop();
			Logger.LogTaskDuration("Prepare coarsest level solver", watch.ElapsedMilliseconds);
		}

		private void PrepareSystemVectors(Vector rhsVector)
		{
			var watch = new Stopwatch();
			watch.Start();

			int numLevels = hierarchy.NumLevels;
			vectorsLhs = new Vector[numLevels];
			vectorsRhs = new Vector[numLevels];
			vectorsWork = new Vector[numLevels];

			// Finest grid. The rest must be done whenever a system is solved
			vectorsWork[0] = Vector.CreateZero(rhsVector.Length);

			// Coarse grids
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				int numDofs = hierarchy.GetSystemMatrix(lvl).NumColumns;
				vectorsRhs[lvl] = Vector.CreateZero(numDofs);
				vectorsLhs[lvl] = Vector.CreateZero(numDofs);
				vectorsWork[lvl] = Vector.CreateZero(numDofs);
			}

			watch.Stop();
			Logger.LogTaskDuration("Prepare system vectors", watch.ElapsedMilliseconds);
		}

		private void RunSingleCycle()
		{
			int lvl = 0; // Each cycle starts at finest grid
			while (true)
			{
				int direction = levelProgression.MoveNext();
				if (direction == 1) // Fine -> coarse
				{
					// Access the correct matrices and vectors
					IReadOnlyMatrix Af = hierarchy.GetSystemMatrix(lvl);
					IReadOnlyMatrix R = hierarchy.GetRestriction(lvl);
					Vector xf = vectorsLhs[lvl];
					Vector bf = vectorsRhs[lvl];
					Vector xc = vectorsLhs[lvl + 1];
					Vector rc = vectorsRhs[lvl + 1]; // Fine residual becomes coarse rhs
					Vector rf = vectorsWork[lvl];

					// MG operations
					smoothers.ApplyPreSmoothing(lvl, bf, xf);
					Af.MultiplyIntoResult(xf, rf);
					rf.LinearCombinationIntoThis(-1, bf, 1);
					R.MultiplyIntoResult(rf, rc);

					// Prepare for next step
					xc.Clear();
					lvl++;
				}
				else if (direction == -1) // Coarse -> fine
				{
					// Access the correct matrices and vectors
					IReadOnlyMatrix P = hierarchy.GetProlongation(lvl - 1);
					Vector ec = vectorsLhs[lvl];
					Vector rc = vectorsRhs[lvl];
					Vector xf = vectorsLhs[lvl - 1];
					Vector ef = vectorsWork[lvl - 1];

					// MG operations
					if (lvl < hierarchy.NumLevels - 1)
					{
						smoothers.ApplyPostSmoothing(lvl, rc, ec);
					}
					else
					{
						coarsestSystemSolver.Solve(rc, ec);
					}

					P.MultiplyIntoResult(ec, ef);
					xf.AddIntoThis(ef);

					// Prepare for next step
					lvl--;
				}
				else if (direction == 0) // Cycle ends here => we are at finest grid
				{
					Debug.Assert(lvl == 0);

					// Access the correct matrices and vectors
					Vector x = vectorsLhs[0];
					Vector b = vectorsRhs[0];

					smoothers.ApplyPostSmoothing(0, b, x);

					// Prepare for next step
					break;
				}
				else
				{
					throw new Exception("Invalid direction in Multigrid cycle schedule");
				}
			}
		}
	}
}
