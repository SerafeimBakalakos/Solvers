namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.DirectSolver;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;

	/// <summary>
	/// Sets up a Geometric Multigrid solver.
	/// </summary>
	public class GmgSolverBuilder
	{
		private readonly int numLevels;
		private readonly IGrid finestGrid;
		private readonly IDofOrderer dofOrderer;
		private readonly IImplementationProvider linearAlgebraProvider;
		private readonly IProlongationStrategy prolongation;

		private MultigridSmoothers smoothers;
		private bool useGalerkinCoarseMatrices = true;

		public GmgSolverBuilder(int numLevels, IGrid finestGrid, IProlongationStrategy prolongation, IImplementationProvider? linearAlgebraProvider = null)
		{
			this.finestGrid = finestGrid;

			if (numLevels < 2) throw new ArgumentException("The must be at least two grids/levels");
			this.numLevels = numLevels;

			if (linearAlgebraProvider is null) linearAlgebraProvider = LibrarySettings.GlobalProvider;
			this.linearAlgebraProvider = linearAlgebraProvider;

			var defaultSmoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(forwardDirection: true), numSteps: 2);
			smoothers = new MultigridSmoothers(numLevels);
			smoothers.DefineSmoother(defaultSmoother);

			CoarsestSystemSolver = new CholeskyCscCoarseSolver(linearAlgebraProvider, new AmdSymmetricOrdering(linearAlgebraProvider));

			this.prolongation = prolongation;
			Restriction = new FullWeightingRestrictionStrategy(finestGrid.Dimension);

			//TODO: Here is where I can enforce that Nodes are numbered in the same way prolongation matrices expect them to.
			//		Ideally I should just reorder them, and thus let the users number them however they want. 
			dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
		}

		public ICoarseSystemSolver CoarsestSystemSolver { get; set; }

		public int[]? CoarseningRatioPerAxis { get; set; } = null;

		public CycleSchedule Cycle { get; set; }

		public IRestrictionStrategy Restriction { get; }

		public GlobalAlgebraicModel<CsrMatrix> BuildAlgebraicModel(IModel model)
			=> new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, new CsrMatrixAssembler());

		public MultigridSolver BuildSolver(GlobalAlgebraicModel<CsrMatrix> model)
		{
			if (CoarseningRatioPerAxis is null)
			{
				CoarseningRatioPerAxis = new int[finestGrid.Dimension];
				Array.Fill(CoarseningRatioPerAxis, 2);
			}

			return new MultigridSolver(model, numLevels, finestGrid, CoarseningRatioPerAxis, prolongation, Restriction, smoothers, CoarsestSystemSolver, Cycle, useGalerkinCoarseMatrices);
		}

		public void ConfigCoarseMatricesGalerkin()
		{
			useGalerkinCoarseMatrices = true;
		}

		public void ConfigCoarseMatricesRediscretization()
		{
			throw new NotImplementedException();
		}

		public void SetSmoothers(IMultigridSmoother commonSmoother)
		{
			this.smoothers.DefineSmoother(commonSmoother);
		}

		public void SetSmoothers(MultigridSmoothers allSmoothers)
		{
			if (allSmoothers.NumLevelsTotal != this.numLevels)
			{
				throw new ArgumentException("The smoothers aggregate must have as many levels as the whole multigrid solver.");
			}

			this.smoothers = allSmoothers;
		}
	}
}
