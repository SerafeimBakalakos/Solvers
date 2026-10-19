namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
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
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class SaAmgSolverBuilder
	{
		private readonly int numLevelsRequested;
		private readonly int maxCycles;

		private readonly INearNullspaceCandidatesRelaxationStrategy[] nullspaceCandidatesRelaxationStrategies;
		private MultigridSmoothers smoothers;

		public SaAmgSolverBuilder(int numLevelsRequested, int maxCycles)
		{
			this.numLevelsRequested = numLevelsRequested;
			this.maxCycles = maxCycles;

			var defaultSmoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(forwardDirection: true), numSteps: 2);
			smoothers = new MultigridSmoothers(numLevelsRequested);
			smoothers.DefineSmoother(defaultSmoother);

			nullspaceCandidatesRelaxationStrategies = new INearNullspaceCandidatesRelaxationStrategy[numLevelsRequested - 1];
			var relaxation = new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4);
			for (int i = 0; i < numLevelsRequested - 1; i++)
			{
				nullspaceCandidatesRelaxationStrategies[i] = relaxation.CopyWithInitialSettings();
			}
		}

		public IAggregationStrategy Aggregation { get; set; } = new StandardAggregation();

		public ICoarseSystemSolver? CoarsestSystemSolver { get; set; }

		public IAmgCoarseningTerminationCriterion CoarseningTermination { get; set; } = new SystemOrderBasedTermination(targetMinOrder: 10);

		public ICycleSchedule CycleSchedule { get; set; } = new VCycleSchedule();

		public bool DiscardIntermediateHierarchyData { get; set; } = true;

		public IImplementationProvider LinearAlgebraProvider { get; set; } = LibrarySettings.GlobalProvider;

		public INearNullspaceCandidatesSelectionStrategy NullspaceCandidatesSelection = new ConstantCandidateSelection();

		public double NullspaceOrthonormalizationTolerance { get; set; } = 1E-10;

		public IProlongationSmoothingStrategy ProlongationSmoothing { get; set; } = new JacobiProlongationSmoothing(weightFactor: 4.0 / 3.0, degree: 1, new SystemRandomRng());

		public double ResidualTolerance = 1E-7;

		public IRestrictionStrategy Restriction { get; set;  } = new SimpleTransposeRestriction();

		public IStrengthOfConnection StrengthOfConnection { get; set; } = new SymmetricStrengthOfConnection(theta: 0.0);

		public SaAmgSolver BuildSolver()
		{
			var candidateFittingStrategy = new CandidateVectorFitting(NullspaceOrthonormalizationTolerance);
			var hierarchy = new SaAmgOperatorHierarchy(numLevelsRequested, CoarseningTermination, StrengthOfConnection, Aggregation, NullspaceCandidatesSelection, nullspaceCandidatesRelaxationStrategies, candidateFittingStrategy, ProlongationSmoothing, Restriction, DiscardIntermediateHierarchyData);

			CoarsestSystemSolver = FinalizeCoarsetSystemSolver();
			return new SaAmgSolver(hierarchy, smoothers, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance);
		}

		public void SetNullspaceCandidatesRelaxationAll(INearNullspaceCandidatesRelaxationStrategy commonRelaxation)
		{
			for (int i = 0; i < numLevelsRequested - 1; i++)
			{
				nullspaceCandidatesRelaxationStrategies[i] = commonRelaxation.CopyWithInitialSettings();
			}
		}

		public void SetNullspaceCandidatesRelaxationAtLevel(int level, INearNullspaceCandidatesRelaxationStrategy relaxation)
		{
			if (level >= numLevelsRequested - 1)
			{
				throw new ArgumentOutOfRangeException("This setting applies to levels other than the last");
			}

			nullspaceCandidatesRelaxationStrategies[level] = relaxation.CopyWithInitialSettings();
		}

		public void SetSmoothers(IMultigridSmoother commonSmoother)
		{
			this.smoothers.DefineSmoother(commonSmoother);
		}

		public void SetSmoothers(MultigridSmoothers allSmoothers)
		{
			if (allSmoothers.NumLevelsTotal != this.numLevelsRequested)
			{
				throw new ArgumentException("The smoothers aggregate must have as many levels as the whole multigrid solver.");
			}

			this.smoothers = allSmoothers;
		}

		private ICoarseSystemSolver FinalizeCoarsetSystemSolver()
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
