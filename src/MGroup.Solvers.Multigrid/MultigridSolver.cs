namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.DirectSolver;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class MultigridSolver : ISolver
	{
		private readonly ICoarseSystemSolver coarsestSystemSolver;
		private readonly ICycleSchedule cycleSchedule;
		private readonly IGrid[] grids;
		private readonly IntergridTransfer[] intergridTransfers;
		private readonly GlobalAlgebraicModel<CsrMatrix> model; // Perhaps I need a dedicated algebraic model for multigrid
		private readonly int numLevels;
		private readonly MultigridSmoothers smoothers;

		internal MultigridSolver(GlobalAlgebraicModel<CsrMatrix> model, int numLevels, IGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, CycleSchedule cycle, bool useGalerkinCoarseMatrices)
		{
			Name = "MultigridSolver";
			Logger = new SolverLogger(Name);
			this.model = model;
			this.LinearSystem = model.LinearSystem;
			LinearSystem.Observers.Add(this);

			this.numLevels = numLevels;
			this.smoothers = smoothers;
			this.coarsestSystemSolver = coarsestSystemSolver;

			// Grids
			var gridDimensions = new GridDimensions(finestGrid.Dimension, numLevels, finestGrid.NumNodesPerAxis, coarseningRatios);
			grids = new IGrid[numLevels];
			for (int lvl = 0; lvl < numLevels; lvl++)
			{
				grids[lvl] = gridDimensions.MakeGridForLevel(lvl);
			}

			// Prolongations, restrictions
			intergridTransfers = new IntergridTransfer[numLevels - 1];
			for (int lvl = 0; lvl < numLevels - 1; lvl++)
			{
				intergridTransfers[lvl] = new IntergridTransfer(prolongationStrategy, restrictionStrategy);
			}

			// Cycle schedule
			if (cycle == CycleSchedule.VCycle)
			{
				this.cycleSchedule = VCycleSchedule.Create(numLevels);
			}
			else if (cycle == CycleSchedule.WCycle)
			{
				this.cycleSchedule = WCycleSchedule.Create(numLevels);
			}
			else
			{
				this.cycleSchedule = FCycleSchedule.Create(numLevels);
			}
		}

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public ISolverLogger Logger { get; }

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; set; }

		public string Name { get; }

		public void Initialize() { }

		public void HandleMatrixWillBeSet()
		{
			// Clear everything
			throw new NotImplementedException();
		}

		public void PreventFromOverwrittingSystemMatrices() { }

		public void Solve()
		{

		}
	}
}
