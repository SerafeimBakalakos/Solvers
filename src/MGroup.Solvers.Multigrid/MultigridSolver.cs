namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.CoarseMatrix;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class MultigridSolver : ISolver
	{
		private readonly GlobalAlgebraicModel<CsrMatrix> algebraicModel; // Perhaps I need a dedicated algebraic model for multigrid
		private readonly ICoarseSystemSolver coarsestSystemSolver;
		private readonly ICycleSchedule cycleSchedule;
		private readonly IDofType[] dofsPerNode;
		private readonly IGrid[] grids;
		private readonly IntergridTransfers intergridTransfers;
		private readonly Model model;
		private readonly int maxCycles;
		private readonly int numLevels;
		private readonly double residualTolerance;
		private readonly MultigridSmoothers smoothers;
		private readonly ICoarseMatrixStrategy systemMatrices;

		private readonly Vector[] vectorsLhs;
		private readonly Vector[] vectorsRhs;

		private bool isInitialized = false;
		private bool mustPrepareSystemMatrices = true;
		private LevelProgression levelProgression;

		internal MultigridSolver(GlobalAlgebraicModel<CsrMatrix> algebraicModel, Model model, IDofType[] dofsPerNode, int numLevels, IGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, bool useGalerkinCoarseMatrices)
		{
			Name = "MultigridSolver";
			Logger = new SolverLogger(Name);

			this.model = model;
			this.dofsPerNode = dofsPerNode;
			this.algebraicModel = algebraicModel;
			this.LinearSystem = algebraicModel.LinearSystem;
			LinearSystem.Observers.Add(this);

			this.numLevels = numLevels;
			this.smoothers = smoothers;
			this.coarsestSystemSolver = coarsestSystemSolver;
			this.cycleSchedule = cycleSchedule;
			this.maxCycles = maxCycles;
			this.residualTolerance = residualTolerance;

			// Grids
			var gridDimensions = new GridDimensions(finestGrid.Dimension, numLevels, finestGrid.NumNodesPerAxis, coarseningRatios);
			grids = new IGrid[numLevels];
			for (int lvl = 0; lvl < numLevels; lvl++)
			{
				grids[lvl] = gridDimensions.MakeGridForLevel(lvl);
			}

			// Prolongations, restrictions
			intergridTransfers = new IntergridTransfers(model, dofsPerNode, prolongationStrategy, restrictionStrategy); 

			// Linear system matrices
			if (useGalerkinCoarseMatrices)
			{
				systemMatrices = new GalerkinCoarseMatrixStrategy(numLevels, new GalerkinProductCsr(), intergridTransfers, () => algebraicModel.LinearSystem.Matrix);	 
			}
			else
			{
				throw new NotImplementedException();
			}

			// Vectors
			vectorsLhs = new Vector[numLevels];
			vectorsRhs = new Vector[numLevels];

		}

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public ISolverLogger Logger { get; }

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; set; }

		public string Name { get; }

		public void Initialize() 
		{
			var watch = new Stopwatch();
			watch.Start();

			levelProgression = cycleSchedule.CreateProgression(numLevels);
			ISubdomainFreeDofOrdering freeDofOrderingFinest = algebraicModel.SubdomainFreeDofOrdering;
			intergridTransfers.Initialize(grids, freeDofOrderingFinest, freeDofOrderingFinest.AllDofs);

			watch.Stop();
			Logger.LogTaskDuration("Initialization", watch.ElapsedMilliseconds);
		}

		public void HandleMatrixWillBeSet()
		{
			coarsestSystemSolver.Clear();
			systemMatrices.Clear();
			mustPrepareSystemMatrices = true;

			Array.Clear(vectorsRhs, 0, numLevels);
			Array.Clear(vectorsLhs, 0, numLevels);

			intergridTransfers.Clear(); //TODO: This applies only for AMG though
		}

		public void PreventFromOverwrittingSystemMatrices() { }

		public void Solve()
		{
			if (!isInitialized)
			{
				Initialize();
				isInitialized = true;
			}

			if (mustPrepareSystemMatrices)
			{
				PrepareSystemMatrices();
				PrepareSystemVectors();
				mustPrepareSystemMatrices = false;
			}

			var watch = new Stopwatch();
			watch.Start();

			vectorsRhs[0] = LinearSystem.RhsVector;
			vectorsLhs[0] = LinearSystem.Solution;

			Vector b = algebraicModel.LinearSystem.RhsVector;
			var res = Vector.CreateZero(b.Length);
			double normRes0 = b.Norm2();
			double resRatio = 1.0;
			int cycleIdx;
			for (cycleIdx = 0; cycleIdx < maxCycles; cycleIdx++)
			{
				RunSingleCycle();

				// Calculate the residual of the coarsest grid.
				//TODO: Perhaps this is already done inside the RunSingleCycle().
				Vector x = algebraicModel.LinearSystem.Solution;
				IReadOnlyMatrix matrix = algebraicModel.LinearSystem.Matrix;
				matrix.MultiplyIntoResult(x, res);
				res.LinearCombinationIntoThis(-1, b, 1);
				double normRes = res.Norm2();
				resRatio = normRes / normRes0;
				

				if (resRatio <= residualTolerance)
				{
					break;
				}
			}

			watch.Stop();
			Logger.LogIterativeAlgorithm(cycleIdx + 1, resRatio);
			Logger.LogTaskDuration("Execution of cycles", watch.ElapsedMilliseconds);

			if (resRatio > residualTolerance)
			{
				throw new IterativeSolverNotConvergedException(
					$"{Name} did not converge to a solution. The solver ran for {maxCycles} cycles iterations and ||b-A*x|| / ||b|| is {resRatio}.");
			}
		}

		private void PrepareSystemMatrices()
		{
			var watch = new Stopwatch();
			watch.Start();

			systemMatrices.CalcCoarseSystemMatrices();
			for (int lvl = 0; lvl < numLevels - 1;  lvl++)
			{
				smoothers.Update(lvl, systemMatrices.GetLinearSystemMatrix(lvl), areDofsModified: true);
			}
			coarsestSystemSolver.Initialize(systemMatrices.GetLinearSystemMatrix(numLevels - 1));

			watch.Stop();
			Logger.LogTaskDuration("Preparation of system matrices", watch.ElapsedMilliseconds);
		}

		private void PrepareSystemVectors()
		{
			var watch = new Stopwatch();
			watch.Start();

			// Do not allocate memory for the finest level, since the rhs and solution vector of the original system will be used.
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				int numDofs = systemMatrices.GetLinearSystemMatrix(lvl).NumColumns;
				vectorsRhs[lvl] = Vector.CreateZero(numDofs);
				vectorsLhs[lvl] = Vector.CreateZero(numDofs);
			}

			watch.Stop();
			Logger.LogTaskDuration("Preparation of system vectors", watch.ElapsedMilliseconds);
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
					IReadOnlyMatrix Af = systemMatrices.GetLinearSystemMatrix(lvl);
					IReadOnlyMatrix R = intergridTransfers.GetRestriction(lvl);
					Vector xf = vectorsLhs[lvl];
					Vector bf = vectorsRhs[lvl];
					Vector xc = vectorsLhs[lvl + 1];
					Vector rc = vectorsRhs[lvl + 1]; // Fine residual becomes coarse rhs

					// MG operations
					smoothers.ApplyPreSmoothing(lvl, bf, xf);
					var rf = Vector.CreateZero(bf.Length); //TODO: Preallocate this as work array or differentiate between r and b.
					Af.MultiplyIntoResult(xf, rf);
					rf.LinearCombinationIntoThis(-1, bf, 1);
					R.MultiplyIntoResult(rf, rc);

					// Prepare for next step
					xc.Clear(); //TODO: I think this is needed only when smoothing in the coarse level. If so, move it to that if case.
					lvl++;
				}
				else if (direction == -1) // Coarse -> fine
				{
					// Access the correct matrices and vectors
					IReadOnlyMatrix P = intergridTransfers.GetProlongation(lvl-1);
					Vector ec = vectorsLhs[lvl];
					Vector rc = vectorsRhs[lvl];
					Vector xf = vectorsLhs[lvl - 1];

					// MG operations
					if (lvl < numLevels - 1)
					{
						smoothers.ApplyPostSmoothing(lvl, rc, ec);
					}
					else
					{
						coarsestSystemSolver.Solve(rc, ec);
					}

					Vector ef = xf.Copy(); //TODO: Preallocate this as work array or differentiate between e and x.
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
