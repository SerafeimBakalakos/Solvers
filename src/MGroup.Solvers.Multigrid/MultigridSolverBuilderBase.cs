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
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Restriction;
	using MGroup.Solvers.Multigrid.Geometric.Grids;
	using MGroup.Solvers.Multigrid.Geometric.Prolongations;

	/// <summary>
	/// Sets up a Multigrid solver.
	/// </summary>
	public class MultigridSolverBuilderBase
	{
		protected readonly int numLevels;
		protected readonly IUniformGrid finestGrid;
		protected readonly IDofOrderer dofOrderer;
		protected readonly int maxCycles;
		protected readonly IGeometricProlongationStrategy prolongation;

		protected MultigridSmoothersHierarchy smoothersHierarchy;
		protected bool useGalerkinCoarseMatrices = true;

		public MultigridSolverBuilderBase(int numLevels, IUniformGrid finestGrid, IGeometricProlongationStrategy prolongation, int maxCycles)
		{
			this.finestGrid = finestGrid;

			if (numLevels < 2) throw new ArgumentException("The must be at least two grids/levels");
			this.numLevels = numLevels;

			var defaultSmoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(forwardDirection: true), numSteps: 2);
			smoothersHierarchy = new MultigridSmoothersHierarchy(numLevels);
			smoothersHierarchy.DefineSmoother(defaultSmoother);

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
			this.smoothersHierarchy.DefineSmoother(commonSmoother);
		}

		public void SetSmoothers(MultigridSmoothersHierarchy allSmoothers)
		{
			if (allSmoothers.NumLevelsTotal != this.numLevels)
			{
				throw new ArgumentException("The smoothers aggregate must have as many levels as the whole multigrid solver.");
			}

			this.smoothersHierarchy = allSmoothers;
		}

		protected int[] FinalizeCoarseningRatios()
		{
			if (CoarseningRatioPerAxis is null)
			{
				CoarseningRatioPerAxis = new int[finestGrid.Dimension];
				Array.Fill(CoarseningRatioPerAxis, 2);
			}
			return CoarseningRatioPerAxis;
		}

		protected ICoarseSystemSolver FinalizeCoarsetSystemSolver()
		{
			if (CoarsestSystemSolver is null)
			{
				var reordering = new AmdSymmetricOrdering(LinearAlgebraProvider);
				CoarsestSystemSolver = new CholeskyCscCoarseSolver(LinearAlgebraProvider, reordering);
			}

			return CoarsestSystemSolver;
		}
	}
}
