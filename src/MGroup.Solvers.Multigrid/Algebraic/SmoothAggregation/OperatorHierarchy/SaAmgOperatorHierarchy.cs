namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseMatrix;

	public class SaAmgOperatorHierarchy
	{
		private readonly int numLevelsRequested;
		private readonly bool discardIntermediateData;

		private readonly IAggregationStrategy aggregationStrategy;
		private readonly CandidateVectorFitting candidatesFitting;
		private readonly INearNullspaceCandidatesRelaxationStrategy[] candidatesRelaxationStrategies;
		private readonly INearNullspaceCandidatesSelectionStrategy candidatesSelectionStrategy;
		private readonly GalerkinProductCsr galerkinProductStrategy = new();
		private readonly IProlongationSmoothingStrategy prolongationSmoothingStrategy;
		private readonly IRestrictionStrategy restrictionStrategy;
		private readonly IStrengthOfConnection strengthOfConnectionStrategy;
		private readonly IAmgCoarseningTerminationCriterion terminationCriterion;

		// Necessary
		private readonly List<CsrMatrix> prolongationsPerLevel;
		private readonly List<CsrMatrix> restrictionsPerLevel;
		private readonly List<CsrMatrix> systemMatricesPerLevel;

		// Optional
		private readonly List<AggregateCollection> aggregatesPerLevel;
		private readonly List<Matrix> nullspaceCandidatesPerLevel;
		private readonly List<CsrMatrix> strengthOfConnectionsPerLevel;
		private readonly List<CsrMatrix> tentativeProlongationsPerLevel;

		public SaAmgOperatorHierarchy(int numLevelsRequested, IAmgCoarseningTerminationCriterion terminationCriterion, IStrengthOfConnection strengthOfConnectionStrategy, IAggregationStrategy aggregationStrategy, INearNullspaceCandidatesSelectionStrategy candidatesSelectionStrategy, IList<INearNullspaceCandidatesRelaxationStrategy> candidatesRelaxationStrategies, CandidateVectorFitting candidatesFitting, IProlongationSmoothingStrategy prolongationSmoothingStrategy, IRestrictionStrategy restrictionStrategy, bool discardIntermediateData = true)
		{
			if (numLevelsRequested < 1) throw new ArgumentOutOfRangeException("There must be at least 1 level.");
			if (candidatesRelaxationStrategies.Count != numLevelsRequested - 1)
			{
				throw new ArgumentException("Caller must provide one nullspace candidate relaxation strategies for each level, other than the last");
			}
			this.numLevelsRequested = numLevelsRequested;
			this.terminationCriterion = terminationCriterion;
			this.strengthOfConnectionStrategy = strengthOfConnectionStrategy;
			this.aggregationStrategy = aggregationStrategy;
			this.candidatesSelectionStrategy = candidatesSelectionStrategy;
			this.candidatesRelaxationStrategies = candidatesRelaxationStrategies.ToArray();
			this.candidatesFitting = candidatesFitting;
			this.prolongationSmoothingStrategy = prolongationSmoothingStrategy;
			this.restrictionStrategy = restrictionStrategy;
			this.discardIntermediateData = discardIntermediateData;

			prolongationsPerLevel = new List<CsrMatrix>(numLevelsRequested);
			restrictionsPerLevel = new List<CsrMatrix>(numLevelsRequested);
			systemMatricesPerLevel = new List<CsrMatrix>(numLevelsRequested);

			aggregatesPerLevel = new List<AggregateCollection>(numLevelsRequested);
			nullspaceCandidatesPerLevel = new List<Matrix>(numLevelsRequested);
			strengthOfConnectionsPerLevel = new List<CsrMatrix>(numLevelsRequested);
			tentativeProlongationsPerLevel = new List<CsrMatrix>(numLevelsRequested);
		}

		public int NumLevels { get; private set; }

		public void BuildOperators(CsrMatrix finestLevelSystemMatrix)
		{
			CsrMatrix Af = finestLevelSystemMatrix;
			systemMatricesPerLevel.Add(Af);
			Matrix Bf = candidatesSelectionStrategy.ChooseCandidateVectors(Af);
			nullspaceCandidatesPerLevel.Add(Bf);

			int lvl = 0;
			while ((lvl < numLevelsRequested - 1) && !terminationCriterion.MustStopCoarsening(Af))
			{
				// Aggregation
				CsrMatrix C = strengthOfConnectionStrategy.CalcStrengthMatrix(Af);
				if (!discardIntermediateData) strengthOfConnectionsPerLevel.Add(C);

				AggregateCollection aggregates = aggregationStrategy.FindAggregates(C);
				if (!discardIntermediateData) aggregatesPerLevel.Add(aggregates);

				// Smooth prolongation
				candidatesRelaxationStrategies[lvl].ImproveCandidateVectors(Af, Bf);
				// B is already stored

				(CsrMatrix Pt, Matrix Bc) = candidatesFitting.FitCandidates(aggregates, Bf);
				if (!discardIntermediateData)
				{
					tentativeProlongationsPerLevel.Add(Pt);
					nullspaceCandidatesPerLevel.Add(Bc);
				}

				CsrMatrix P = prolongationSmoothingStrategy.SmoothProlongationMatrix(Pt, Af);
				prolongationsPerLevel.Add(P);

				// Restriction and system matrix coarsening
				CsrMatrix R = restrictionStrategy.CalcRestrictionMatrix(P);
				restrictionsPerLevel.Add(R);

				CsrMatrix Ac = galerkinProductStrategy.CalcProduct(R, Af, P);
				systemMatricesPerLevel.Add(Ac);

				// Prepare for next level
				Af = Ac;
				Bf = Bc;
				lvl++;
			}

			NumLevels = lvl + 1;
		}

		public CsrMatrix GetProlongation(int level)
		{
			CheckFineLevel(level);
			return prolongationsPerLevel[level];
		}

		public CsrMatrix GetRestriction(int level)
		{
			CheckFineLevel(level);
			return restrictionsPerLevel[level];
		}

		public CsrMatrix GetSystemMatrix(int level) => systemMatricesPerLevel[level];

		public AggregateCollection GetAggregates(int level)
		{
			CheckOptionalData();
			CheckFineLevel(level);
			return aggregatesPerLevel[level];
		}

		public Matrix GetNullspaceCandidates(int level)
		{
			CheckOptionalData();
			return nullspaceCandidatesPerLevel[level];
		}

		public CsrMatrix GetStrengthOfConnectionMatrix(int level)
		{
			CheckOptionalData();
			CheckFineLevel(level);
			return strengthOfConnectionsPerLevel[level];
		}

		public CsrMatrix GetTentativeProlongation(int level)
		{
			CheckOptionalData();
			CheckFineLevel(level);
			return tentativeProlongationsPerLevel[level];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckFineLevel(int level)
		{
			if (level >= NumLevels - 1) throw new ArgumentException("This data is stored only for levels corresponding to fine-grids");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckOptionalData()
		{
			if (discardIntermediateData) throw new InvalidOperationException("Intermediate data is not stored");
		}
	}
}
