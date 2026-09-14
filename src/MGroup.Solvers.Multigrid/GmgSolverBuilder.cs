namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;

	/// <summary>
	/// Sets up a Geometric Multigrid solver.
	/// </summary>
	public class GmgSolverBuilder
	{
		private readonly int numLevels;
		private readonly IGrid finestGrid;
		private readonly IDofOrderer dofOrderer;
		private readonly int maxCycles;
		private readonly IProlongationStrategy prolongation;
		
		private MultigridSmoothers smoothers;
		private bool useGalerkinCoarseMatrices = true;

		public GmgSolverBuilder(int numLevels, IGrid finestGrid, IProlongationStrategy prolongation, int maxCycles)
		{
			this.finestGrid = finestGrid;

			if (numLevels < 2) throw new ArgumentException("The must be at least two grids/levels");
			this.numLevels = numLevels;

			var defaultSmoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(forwardDirection: true), numSteps: 2);
			smoothers = new MultigridSmoothers(numLevels);
			smoothers.DefineSmoother(defaultSmoother);

			this.prolongation = prolongation;
			this.maxCycles = maxCycles;
			Restriction = new FullWeightingRestrictionStrategy(finestGrid.Dimension);

			//TODO: Here is where I can enforce that Nodes are numbered in the same way prolongation matrices expect them to.
			//		Ideally I should just reorder them, and thus let the users number them however they want. 
			dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
		}

		public ICoarseSystemSolver? CoarsestSystemSolver { get; set; }

		public int[]? CoarseningRatioPerAxis { get; set; } = null;

		public ICycleSchedule CycleSchedule { get; set; } = new VCycleSchedule();
		
		public double DropToleranceForSmallEntriesOfSystemMatrix { get; set; } = -1;

		public IImplementationProvider LinearAlgebraProvider { get; set; } = LibrarySettings.GlobalProvider;


		public double ResidualTolerance = 1E-7;

		public IRestrictionStrategy Restriction { get; }

		public GlobalAlgebraicModel<CsrMatrix> BuildAlgebraicModel(Model model)
		{
			var assembler = new CsrMatrixAssembler(isMatrixSymmetric: true);
			assembler.SortColsOfEachRow = true;
			assembler.DropEntryTolerance = DropToleranceForSmallEntriesOfSystemMatrix;
			return new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, assembler);
		}

		public MultigridSolver BuildSolver(Model model, IDofType[] dofsPerNode, GlobalAlgebraicModel<CsrMatrix> algebraicModel)
		{
			if (CoarseningRatioPerAxis is null)
			{
				CoarseningRatioPerAxis = new int[finestGrid.Dimension];
				Array.Fill(CoarseningRatioPerAxis, 2);
			}

			if (CoarsestSystemSolver is null)
			{
				var reordering = new AmdSymmetricOrdering(LinearAlgebraProvider);
				CoarsestSystemSolver = new CholeskyCscCoarseSolver(LinearAlgebraProvider, reordering);
			}

			return new MultigridSolver(algebraicModel, model, dofsPerNode, numLevels, finestGrid, CoarseningRatioPerAxis, prolongation, Restriction, smoothers, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance, useGalerkinCoarseMatrices);
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
