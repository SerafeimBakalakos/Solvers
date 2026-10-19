namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SaAmgOperatorHierarchyTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			int numLevelsRequested = 5;
			double socTheta = 0.0;
			double orthonormalizationTolerance = 1E-10;
			double smoothingDampingFactor = 4.0 / 3.0;
			int smoothingDegree = 1;
			string relaxationStrategy = "SGS-4";

			CsrMatrix csrA = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);

			var terminationCriterion = new SystemOrderBasedTermination(10);
			var socStrategy = new SymmetricStrengthOfConnection(socTheta);
			var aggregationStrategy = new StandardAggregation();
			var candidateSelectionStrategy = new ConstantCandidateSelection();
			var candidateFittingStrategy = new CandidateVectorFitting(orthonormalizationTolerance);
			IJacobiWeightingStrategy weightingStrategy = new SpectralRadiusJacobiWeighting(dampingFactor: 4.0 / 3.0, new SystemRandomRng(seed: 13));
			var prolongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, smoothingDegree);
			var restrictionStrategy = new SimpleTransposeRestriction();

			var relaxation = new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4);
			var candidateRelaxationStrategies = new List<INearNullspaceCandidatesRelaxationStrategy>();
			for (int i = 0; i < numLevelsRequested - 1; i++)
			{
				candidateRelaxationStrategies.Add(relaxation.CopyWithInitialSettings());
			}

			var hierarchy = new SaAmgOperatorHierarchy(numLevelsRequested, terminationCriterion, socStrategy, aggregationStrategy, candidateSelectionStrategy, candidateRelaxationStrategies, candidateFittingStrategy, prolongationSmoothing, restrictionStrategy, discardIntermediateData: false);
			hierarchy.BuildOperators(csrA);

			int expectedNumLevels = 2;
			CsrMatrix expectedA0 = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(0);
			CsrMatrix expectedA1 = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(1);
			(int[] aggregatesOfFineDofs, int[] fineDofRootsOfAggregates, _) = Poisson1D12ElementsSaAmgResults.GetAggregates(level: 0);
			var expectedAggregates = new AggregateCollection(aggregatesOfFineDofs, fineDofRootsOfAggregates);
			CsrMatrix expectedC0 = MatrixUtilities.ArrayToCsr(Poisson1D12ElementsSaAmgResults.GetStrengthOfConnectionMatrix(0, socTheta));
			Matrix expectedB0 = Matrix.CreateFromArray(Poisson1D12ElementsSaAmgResults.GetNullspaceCandidates(0, relaxationStrategy));
			Matrix expectedB1 = Matrix.CreateFromArray(Poisson1D12ElementsSaAmgResults.GetNullspaceCandidates(1, relaxationStrategy));
			CsrMatrix expectedPt0 = Poisson1D12ElementsSaAmgResults.GetTentativeProlongation(0, relaxationStrategy);
			CsrMatrix expectedP0 = Poisson1D12ElementsSaAmgResults.GetProlongationMatrix(0, weightingStrategy:  "SpectralRadius", smoothingDampingFactor, smoothingDegree);
			CsrMatrix expectedR0 = Poisson1D12ElementsSaAmgResults.GetRestrictionMatrix(0);

			int computedNumLevels = hierarchy.NumLevels;
			CsrMatrix computedA0 = hierarchy.GetSystemMatrix(0);
			CsrMatrix computedA1 = hierarchy.GetSystemMatrix(1);
			AggregateCollection computedAggregates = hierarchy.GetAggregates(0);
			CsrMatrix computedC0 = hierarchy.GetStrengthOfConnectionMatrix(0);
			Matrix computedB0 = hierarchy.GetNullspaceCandidates(0);
			Matrix computedB1 = hierarchy.GetNullspaceCandidates(1);
			CsrMatrix computedPt0 = hierarchy.GetTentativeProlongation(0);
			CsrMatrix computedP0 = hierarchy.GetProlongation(0);
			CsrMatrix computedR0 = hierarchy.GetRestriction(0);

			var comparer = new MatrixComparer(tolerance: 1E-30);
			Assert.Equal(expectedNumLevels, computedNumLevels);
			AssertUtilities.AssertEqualCsr(expectedA0, computedA0, tolerance: 1E-30);
			AssertUtilities.AssertEqualCsr(expectedA1, computedA1, tolerance: 1E-15);
			Assert.Equal(expectedAggregates.NumAggregates, computedAggregates.NumAggregates);
			Assert.Equal(expectedAggregates.AggregatesOfFineDofs, computedAggregates.AggregatesOfFineDofs);
			Assert.Equal(expectedAggregates.FineDofRootsOfAggregates, computedAggregates.FineDofRootsOfAggregates);
			AssertUtilities.AssertEqualCsr(expectedC0, computedC0, tolerance: 1E-30);
			comparer.AssertEqual(expectedB0, computedB0);
			comparer.AssertEqual(expectedB1, computedB1);
			AssertUtilities.AssertEqualCsr(expectedPt0, computedPt0, tolerance: 1E-30);
			AssertUtilities.AssertEqualCsr(expectedP0, computedP0, tolerance: 1E-14);
			AssertUtilities.AssertEqualCsr(expectedR0, computedR0, tolerance: 1E-14);
		}
	}
}
