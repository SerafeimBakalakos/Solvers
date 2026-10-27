namespace MGroup.Solvers.Multigrid.Algorithms
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
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Smoothing;

	public abstract class MultigridAlgorithmBase : IMultigridAlgorithm
	{
		protected readonly ICoarseSystemSolver coarsestSystemSolver;
		protected readonly ICycleSchedule cycleSchedule;
		protected IMultigridOperatorHierarchy operatorHierarchy;
		protected readonly int maxCycles;
		protected readonly double residualTolerance;
		protected readonly MultigridSmoothersHierarchy smoothersHierarchy;
		protected readonly MultigridVectorHierarchy vectorHierarchy = new();

		protected LevelProgression levelProgression;

		public MultigridAlgorithmBase(IMultigridOperatorHierarchy operatorHierarchy, MultigridSmoothersHierarchy smoothersHierarchy, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, string name)
		{
			Name = name;
			Logger = new MultigridSolverLogger(name);

			this.operatorHierarchy = operatorHierarchy;
			this.smoothersHierarchy = smoothersHierarchy;
			this.coarsestSystemSolver = coarsestSystemSolver;
			this.cycleSchedule = cycleSchedule;
			this.maxCycles = maxCycles;
			this.residualTolerance = residualTolerance;
		}

		public MultigridSolverLogger Logger { get; }

		public string Name { get; }
		
		public IMultigridOperatorHierarchy OperatorHierarchy => operatorHierarchy;

		public void PrepareForNewLinearSystemMatrix(CsrMatrix systemMatrix)
		{
			PrepareOperatorHierarchy(systemMatrix);
			PrepareLevelProgression();

			int numLevels = operatorHierarchy.NumLevels;

			Logger.ExecuteTimeAndLog("Prepare smoothers", () =>
			{
				if (smoothersHierarchy.NumLevelsTotal < numLevels)
				{
					throw new Exception($"Not enough smoothers are provided for the {numLevels} actual levels.");
				}
				for (int lvl = 0; lvl < numLevels - 1; lvl++)
				{
					smoothersHierarchy.Update(lvl, operatorHierarchy.GetSystemMatrix(lvl), areDofsModified: true);
				}
			});

			Logger.ExecuteTimeAndLog("Prepare coarsest level solver", () =>
			{
				coarsestSystemSolver.Initialize(operatorHierarchy.GetSystemMatrix(numLevels - 1));
			});

			Logger.ExecuteTimeAndLog("Prepare system vectors", () =>
			{
				vectorHierarchy.AllocateVectors(operatorHierarchy);
			});
		}

		public void RunSingleCycle(Vector rhsVector, Vector solution)
		{
			vectorHierarchy.SetFinestLevelVectors(rhsVector, solution);
			RunCycle();
		}

		public void SolveLinearSystem(Vector rhsVector, Vector solution)
		{
			Logger.ExecuteTimeAndLog("Prepare system vectors", () =>
			{
				vectorHierarchy.AllocateVectors(operatorHierarchy);
			});

			var watch = new Stopwatch();
			watch.Start();

			CsrMatrix finestSystemMatrix = operatorHierarchy.GetSystemMatrix(0);
			vectorHierarchy.SetFinestLevelVectors(rhsVector, solution);

			var residualHistory = new ResidualNormHistory();
			var res = Vector.CreateZero(rhsVector.Length);
			double normRes0 = rhsVector.Norm2();
			residualHistory.LogInitialResidual(normRes0);

			double resRatio = 1.0;
			int cycleIdx;
			for (cycleIdx = 0; cycleIdx < maxCycles; cycleIdx++)
			{
				RunCycle();

				// Calculate the residual of the coarsest grid.
				//TODO: Perhaps this is already done inside the RunSingleCycle().
				finestSystemMatrix.MultiplyIntoResult(solution, res);
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

		private void RunCycle()
		{
			int lvl = 0; // Each cycle starts at finest grid
			while (true)
			{
				int direction = levelProgression.MoveNext();
				if (direction == 1) // Fine -> coarse
				{
					// Access the correct matrices and vectors
					IReadOnlyMatrix Af = operatorHierarchy.GetSystemMatrix(lvl);
					IReadOnlyMatrix R = operatorHierarchy.GetRestriction(lvl);
					Vector xf = vectorHierarchy.GetLhsVector(lvl);
					Vector bf = vectorHierarchy.GetRhsVector(lvl);
					Vector xc = vectorHierarchy.GetLhsVector(lvl + 1);
					Vector rc = vectorHierarchy.GetRhsVector(lvl + 1); // Fine residual becomes coarse rhs
					Vector rf = vectorHierarchy.GetWorkVector(lvl);

					// MG operations
					smoothersHierarchy.ApplyPreSmoothing(lvl, bf, xf);
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
					IReadOnlyMatrix P = operatorHierarchy.GetProlongation(lvl - 1);
					Vector ec = vectorHierarchy.GetLhsVector(lvl);
					Vector rc = vectorHierarchy.GetRhsVector(lvl);
					Vector xf = vectorHierarchy.GetLhsVector(lvl - 1);
					Vector ef = vectorHierarchy.GetWorkVector(lvl - 1);

					// MG operations
					if (lvl < operatorHierarchy.NumLevels - 1)
					{
						smoothersHierarchy.ApplyPostSmoothing(lvl, rc, ec);
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
					Vector x = vectorHierarchy.GetLhsVector(0);
					Vector b = vectorHierarchy.GetRhsVector(0);

					smoothersHierarchy.ApplyPostSmoothing(0, b, x);

					// Prepare for next step
					break;
				}
				else
				{
					throw new Exception("Invalid direction in Multigrid cycle schedule");
				}
			}
		}

		public abstract void Initialize();

		protected abstract void PrepareLevelProgression();

		protected abstract void PrepareOperatorHierarchy(CsrMatrix systemMatrix);
	}
}
