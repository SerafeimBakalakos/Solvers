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
	using MGroup.Solvers.Multigrid.GalerkinProducts;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Restriction;
	using MGroup.Solvers.Multigrid.Geometric.Grids;
	using MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Geometric.Prolongations;

	public abstract class MultigridSolverBase : ISolver
	{
		protected readonly GlobalAlgebraicModel<CsrMatrix> algebraicModel; // Perhaps I need a dedicated algebraic model for multigrid
		protected readonly ICoarseSystemSolver coarsestSystemSolver;
		protected readonly ICycleSchedule cycleSchedule;
		protected readonly IDofType[] dofsPerNode;
		protected readonly IGridHierarchy gridHierarchy;
		protected readonly Model model;
		protected readonly int maxCycles;
		private readonly GmgOperatorHierarchyGalerkin operatorHierarchy;
		protected readonly double residualTolerance;
		protected readonly MultigridSmoothersHierarchy smoothersHierarchy;

		protected readonly Vector[] vectorsLhs;
		protected readonly Vector[] vectorsRhs;
		protected readonly Vector[] vectorsWork;

		protected bool isInitialized = false;
		protected bool mustRebuildHierarchy = true;
		protected LevelProgression levelProgression;

		internal MultigridSolverBase(GlobalAlgebraicModel<CsrMatrix> algebraicModel, Model model, IDofType[] dofsPerNode, int numLevelsRequested, IUniformGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothersHierarchy smoothersHierarchy, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, bool useGalerkinCoarseMatrices, string name)
		{
			Name = name;
			Logger = new SolverLogger(Name);

			this.model = model;
			this.dofsPerNode = dofsPerNode;
			this.algebraicModel = algebraicModel;
			this.LinearSystem = algebraicModel.LinearSystem;
			LinearSystem.Observers.Add(this);

			this.smoothersHierarchy = smoothersHierarchy;
			this.coarsestSystemSolver = coarsestSystemSolver;
			this.cycleSchedule = cycleSchedule;
			this.maxCycles = maxCycles;
			this.residualTolerance = residualTolerance;

			// Dofs //TODO: inject this
			ISubdomainFreeDofOrdering freeDofOrderingFinest = algebraicModel.SubdomainFreeDofOrdering;
			var freeDofProvider = new StandardFreeDofProvider(model, dofsPerNode, () => algebraicModel.SubdomainFreeDofOrdering);

			// Grids
			gridHierarchy = new UniformGridHierarchy(numLevelsRequested, finestGrid, coarseningRatios);
			gridHierarchy.DefineGrids(); // could go into Initialize.

			// Prolongations, restrictions
			operatorHierarchy = new GmgOperatorHierarchyGalerkin(gridHierarchy, freeDofProvider, prolongationStrategy, restrictionStrategy);

			// Vectors
			vectorsLhs = new Vector[gridHierarchy.NumLevels];
			vectorsRhs = new Vector[gridHierarchy.NumLevels];
			vectorsWork = new Vector[gridHierarchy.NumLevels];
		}

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public ISolverLogger Logger { get; }

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; set; }

		public string Name { get; }

		public void Initialize() 
		{
			var watch = new Stopwatch();
			watch.Start();

			levelProgression = cycleSchedule.CreateProgression(gridHierarchy.NumLevels);

			watch.Stop();
			Logger.LogTaskDuration("Initialization", watch.ElapsedMilliseconds);
		}

		public void HandleMatrixWillBeSet()
		{
			coarsestSystemSolver.Clear();
			operatorHierarchy.Clear(); //TODO: This applies only for AMG though
			mustRebuildHierarchy = true;

			Array.Clear(vectorsRhs, 0, vectorsRhs.Length);
			Array.Clear(vectorsLhs, 0, vectorsLhs.Length);
		}

		public void PreventFromOverwrittingSystemMatrices() { }

		protected void PrepareForNewLinearSystem()
		{
			if (!isInitialized)
			{
				Initialize();
				isInitialized = true;
			}

			if (mustRebuildHierarchy)
			{
				PrepareSystemMatrices();
				PrepareSystemVectors();
				mustRebuildHierarchy = false;
			}
		}

		protected void PrepareSystemMatrices()
		{
			var watch = new Stopwatch();
			watch.Start();

			// System matrices, prolongations, restrictions
			operatorHierarchy.BuildOperators(LinearSystem.Matrix);

			// Smoothers
			for (int lvl = 0; lvl < gridHierarchy.NumLevels - 1;  lvl++)
			{
				smoothersHierarchy.Update(lvl, operatorHierarchy.GetSystemMatrix(lvl), areDofsModified: true);
			}

			// Coarsest level solver
			coarsestSystemSolver.Initialize(operatorHierarchy.GetSystemMatrix(gridHierarchy.NumLevels - 1));

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
			for (int lvl = 1; lvl < gridHierarchy.NumLevels; lvl++)
			{
				int numDofs = operatorHierarchy.GetSystemMatrix(lvl).NumColumns;
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
					IReadOnlyMatrix Af = operatorHierarchy.GetSystemMatrix(lvl);
					IReadOnlyMatrix R = operatorHierarchy.GetRestriction(lvl);
					Vector xf = vectorsLhs[lvl];
					Vector bf = vectorsRhs[lvl];
					Vector xc = vectorsLhs[lvl + 1];
					Vector rc = vectorsRhs[lvl + 1]; // Fine residual becomes coarse rhs
					Vector rf = vectorsWork[lvl];

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
					IReadOnlyMatrix P = operatorHierarchy.GetProlongation(lvl-1);
					Vector ec = vectorsLhs[lvl];
					Vector rc = vectorsRhs[lvl];
					Vector xf = vectorsLhs[lvl - 1];
					Vector ef = vectorsWork[lvl - 1];

					// MG operations
					if (lvl < gridHierarchy.NumLevels - 1)
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
					Vector x = vectorsLhs[0];
					Vector b = vectorsRhs[0];

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

		public abstract void Solve();
	}
}
