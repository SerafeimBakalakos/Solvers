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

	public abstract class MultigridSolverBase : ISolver
	{
		protected readonly GlobalAlgebraicModel<CsrMatrix> algebraicModel; // Perhaps I need a dedicated algebraic model for multigrid
		protected readonly ICoarseSystemSolver coarsestSystemSolver;
		protected readonly ICycleSchedule cycleSchedule;
		protected readonly IDofType[] dofsPerNode;
		protected readonly IGrid[] grids;
		protected readonly IntergridTransfers intergridTransfers;
		protected readonly Model model;
		protected readonly int maxCycles;
		protected readonly int numLevels;
		protected readonly double residualTolerance;
		protected readonly MultigridSmoothers smoothers;
		protected readonly ICoarseMatrixStrategy systemMatrices;

		protected readonly Vector[] vectorsLhs;
		protected readonly Vector[] vectorsRhs;
		protected readonly Vector[] vectorsWork;

		protected bool isInitialized = false;
		protected bool mustPrepareSystemMatrices = true;
		protected LevelProgression levelProgression;

		internal MultigridSolverBase(GlobalAlgebraicModel<CsrMatrix> algebraicModel, Model model, IDofType[] dofsPerNode, int numLevels, IGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, bool useGalerkinCoarseMatrices, string name)
		{
			Name = name;
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
			grids[0] = finestGrid;
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				int[] numNodesCoarse = gridDimensions.GetNumNodesAtLevel(lvl);
				grids[lvl] = finestGrid.CreateGridWithSameSettings(numNodesCoarse);
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
			vectorsWork = new Vector[numLevels];
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

		protected void PrepareForNewLinearSystem()
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
		}

		protected void PrepareSystemMatrices()
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

		protected void PrepareSystemVectors()
		{
			var watch = new Stopwatch();
			watch.Start();

			// Finest grid. The rest must be done whenever a system is solved
			vectorsWork[0] = Vector.CreateZero(LinearSystem.RhsVector.Length);

			// Coarse grids
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				int numDofs = systemMatrices.GetLinearSystemMatrix(lvl).NumColumns;
				vectorsRhs[lvl] = Vector.CreateZero(numDofs);
				vectorsLhs[lvl] = Vector.CreateZero(numDofs);
				vectorsWork[lvl] = Vector.CreateZero(numDofs);
			}

			watch.Stop();
			Logger.LogTaskDuration("Preparation of system vectors", watch.ElapsedMilliseconds);
		}

		protected void RunSingleCycle()
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
					IReadOnlyMatrix P = intergridTransfers.GetProlongation(lvl-1);
					Vector ec = vectorsLhs[lvl];
					Vector rc = vectorsRhs[lvl];
					Vector xf = vectorsLhs[lvl - 1];
					Vector ef = vectorsWork[lvl - 1];

					// MG operations
					if (lvl < numLevels - 1)
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

		public abstract void Solve();
	}
}
