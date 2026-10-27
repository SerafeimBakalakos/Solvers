namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
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
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Algorithms;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class SaAmgAlgorithm : MultigridAlgorithmBase
	{
		private const string name = "Smooth Aggregation AMG";
		private readonly SaAmgOperatorHierarchy saOperatorHierarchy;

		public SaAmgAlgorithm(SaAmgOperatorHierarchy operatorHierarchy, MultigridSmoothersHierarchy smoothersHierarchy, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance)
			: base(operatorHierarchy, smoothersHierarchy, coarsestSystemSolver, cycleSchedule, maxCycles, residualTolerance, name)
		{
			this.saOperatorHierarchy = operatorHierarchy;
			SaAmgLogger = new MultigridSolverLogger<SaAmgHierarchyLog>(Name);
		}

		public override SolverLogger Logger => SaAmgLogger;

		public MultigridSolverLogger<SaAmgHierarchyLog> SaAmgLogger { get; }
		
		public override void Initialize() { } // Everything depends on the system matrix
		
		protected override void PrepareLevelProgression()
		{
			Logger.ExecuteTimeAndLog("Prepare level progression", () =>
			{
				levelProgression = cycleSchedule.CreateProgression(saOperatorHierarchy.NumLevels);
			});
		}

		protected override void PrepareOperatorHierarchy(CsrMatrix systemMatrix)
		{
			Logger.ExecuteTimeAndLog("Prepare prolongations, restrictions and coarse matrices", () =>
			{
				saOperatorHierarchy.BuildOperators(systemMatrix);
			});

			SaAmgLogger.LogOperatorHierarchy(saOperatorHierarchy.Log);
		}

		public class Builder : MultigridAlgorithmBuilderBase
		{
			private readonly INearNullspaceCandidatesRelaxationStrategy[] nullspaceCandidatesRelaxationStrategies;

			public Builder(int numLevelsRequested, int maxCycles)
				: base(numLevelsRequested, maxCycles)
			{
				nullspaceCandidatesRelaxationStrategies = new INearNullspaceCandidatesRelaxationStrategy[numLevelsRequested - 1];
				var relaxation = new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4);
				for (int i = 0; i < numLevelsRequested - 1; i++)
				{
					nullspaceCandidatesRelaxationStrategies[i] = relaxation.CopyWithInitialSettings();
				}

				IJacobiWeightingStrategy weightingStrategy = new SpectralRadiusJacobiWeighting(dampingFactor: 4.0 / 3.0, new SystemRandomRng(seed: 13));
				ProlongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, degree: 1);
			}

			public IAggregationStrategy Aggregation { get; set; } = new StandardAggregation();

			public IAmgCoarseningTerminationCriterion CoarseningTermination { get; set; } = new SystemSizeBasedTermination(targetMinSize: 10);

			public INullspaceCandidatesFittingStrategy NullspaceCandidatesFitting { get; set; } = new ScalarCandidatesFitting(orthonormalizationTolerance: 1E-10);

			public INearNullspaceCandidatesSelectionStrategy NullspaceCandidatesSelection { get; set; } = new ConstantCandidateSelection();

			public IProlongationSmoothingStrategy ProlongationSmoothing { get; set; }

			public IStrengthOfConnection StrengthOfConnection { get; set; } = new SymmetricStrengthOfConnection(theta: 0.0);

			public SaAmgAlgorithm BuildAlgorithm()
			{
				var hierarchy = new SaAmgOperatorHierarchy(numLevelsRequested, CoarseningTermination, StrengthOfConnection, Aggregation, NullspaceCandidatesSelection, nullspaceCandidatesRelaxationStrategies, NullspaceCandidatesFitting, ProlongationSmoothing, Restriction, DiscardIntermediateHierarchyData);

				CoarsestSystemSolver = FinalizeCoarsestSystemSolver();
				return new SaAmgAlgorithm(hierarchy, smoothersHierarchy, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance);
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
		}
	}
}
