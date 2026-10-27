namespace MGroup.Solvers.Multigrid.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Restriction;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class MultigridAlgorithmBuilderBase
	{
		protected readonly int numLevelsRequested;
		protected readonly int maxCycles;

		protected MultigridSmoothersHierarchy smoothersHierarchy;

		public MultigridAlgorithmBuilderBase(int numLevelsRequested, int maxCycles)
		{
			this.numLevelsRequested = numLevelsRequested;
			this.maxCycles = maxCycles;

			var defaultSmoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(forwardDirection: true), numSteps: 2);
			smoothersHierarchy = new MultigridSmoothersHierarchy(numLevelsRequested);
			smoothersHierarchy.DefineSmoother(defaultSmoother);
		}

		public ICoarseSystemSolver? CoarsestSystemSolver { get; set; }

		public ICycleSchedule CycleSchedule { get; set; } = new VCycleSchedule();

		public bool DiscardIntermediateHierarchyData { get; set; } = true;

		public IImplementationProvider LinearAlgebraProvider { get; set; } = LibrarySettings.GlobalProvider;

		public double ResidualTolerance = 1E-7;

		public IRestrictionStrategy RestrictionStrategy { get; set;  } = new UnscaledTransposeRestriction();

		public void SetSmoothers(IMultigridSmoother commonSmoother)
		{
			smoothersHierarchy.DefineSmoother(commonSmoother);
		}

		public void SetSmoothers(MultigridSmoothersHierarchy allSmoothers)
		{
			if (allSmoothers.NumLevelsTotal != numLevelsRequested)
			{
				throw new ArgumentException("The smoothers aggregate must have as many levels as the whole multigrid solver.");
			}

			smoothersHierarchy = allSmoothers;
		}

		protected ICoarseSystemSolver FinalizeCoarsestSystemSolver()
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
