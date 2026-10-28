namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Restriction;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SaAmgOperatorHierarchyTests
	{
		[Fact]
		public static void TestBuildOperators_Cantilever8x4Elements()
		{
			int numLevelsRequested = 5;
			CsrMatrix systemMatrix = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 0);

			var terminationCriterion = new SystemSizeBasedTermination(10);
			var socStrategy = new SymmetricStrengthOfConnection(theta: 0);
			var aggregationStrategy = new StandardAggregation();
			var candidateFittingStrategy = new BlockCandidatesFitting(orthonormalizationTolerance: 1E-10);
			IJacobiWeightingStrategy weightingStrategy = new GershgorinJacobiWeighting(dampingFactor: 4.0 / 3);
			var prolongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, degree: 1);
			var restrictionStrategy = new UnscaledTransposeRestriction();
			
			var relaxation = new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4);
			var candidateRelaxationStrategies = new List<INearNullspaceCandidatesRelaxationStrategy>();
			for (int i = 0; i < numLevelsRequested - 1; i++)
			{
				candidateRelaxationStrategies.Add(relaxation.CopyWithInitialSettings());
			}

			double[,] matrixB = Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(level: 0);
			var candidateSelectionStrategy = new MockElasticityNullspaceCandidateSelection(matrixB);
			//int numElementsY = 4;
			//double Lx = Cantilever2DElasticityProblem.LengthX;
			//double Ly = Cantilever2DElasticityProblem.LengthY;
			//IModel model = Cantilever2DElasticityProblem.CreateModel(numElementsY);
			//ISubdomainFreeDofOrdering dofOrdering = Cantilever2DElasticityProblem.OrderDofs(numElementsY);
			//var candidateSelectionStrategy = new Elasticity2DCartesianCandidateSelection(model, dofOrdering, StructuralDof.TranslationX, StructuralDof.TranslationY, [Lx / 2.0, Ly / 2.0]);

			var hierarchy = new SaAmgOperatorHierarchy(numLevelsRequested, terminationCriterion, socStrategy, aggregationStrategy, candidateSelectionStrategy, candidateRelaxationStrategies, candidateFittingStrategy, prolongationSmoothing, restrictionStrategy, discardIntermediateData: false);
			hierarchy.BuildOperators(systemMatrix);

			int expectedNumLevels = Cantilever2D_8x4Elements_SaAmgResults.NumLevels;
			Assert.Equal(expectedNumLevels, hierarchy.NumLevels);

			var comparer = new MatrixComparer(tolerance: 1E-13);
			for (int lvl = 0; lvl < expectedNumLevels - 1; lvl++)
			{
				CsrMatrix expectedAf = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(lvl);
				(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(lvl);
				var expectedAggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
				CsrMatrix expectedC = Cantilever2D_8x4Elements_SaAmgResults.GetStrengthOfConnectionMatrix(lvl);
				Matrix expectedBf = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(lvl));
				Matrix expectedBc = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(lvl + 1));
				CsrMatrix expectedT = Cantilever2D_8x4Elements_SaAmgResults.GetTentativeProlongation(lvl);
				CsrMatrix expectedP = Cantilever2D_8x4Elements_SaAmgResults.GetProlongation(lvl);
				CsrMatrix expectedR = Cantilever2D_8x4Elements_SaAmgResults.GetRestriction(lvl);
				CsrMatrix expectedAc = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(lvl + 1);

				CsrMatrix computedAf = hierarchy.GetSystemMatrix(lvl);
				AggregateCollection computedAggregates = hierarchy.GetAggregates(lvl);
				CsrMatrix computedC = hierarchy.GetStrengthOfConnectionMatrix(lvl);
				Matrix computedBf = hierarchy.GetNullspaceCandidates(lvl);
				Matrix computedBc = hierarchy.GetNullspaceCandidates(lvl + 1);
				CsrMatrix computedT = hierarchy.GetTentativeProlongation(lvl);
				CsrMatrix computedP = hierarchy.GetProlongation(lvl);
				CsrMatrix computedR = hierarchy.GetRestriction(lvl);
				CsrMatrix computedAc = hierarchy.GetSystemMatrix(lvl + 1);

				AssertUtilities.AssertEqualCsr(expectedAf, computedAf, tolerance: 1E-12);
				Assert.Equal(expectedAggregates.NumAggregates, computedAggregates.NumAggregates);
				Assert.Equal(expectedAggregates.AggregatesOfGraphNodes, computedAggregates.AggregatesOfGraphNodes);
				Assert.Equal(expectedAggregates.RootGraphNodesOfAggregates, computedAggregates.RootGraphNodesOfAggregates);
				AssertUtilities.AssertEqualCsr(expectedC, computedC, tolerance: 1E-15);
				comparer.AssertEqual(expectedBf, computedBf);
				comparer.AssertEqual(expectedBc, computedBc);
				AssertUtilities.AssertEqualCsr(expectedT, computedT, tolerance: 1E-12);
				AssertUtilities.AssertEqualCsr(expectedP, computedP, tolerance: 1E-13);
				AssertUtilities.AssertEqualCsr(expectedR, computedR, tolerance: 1E-13);
				AssertUtilities.AssertEqualCsr(expectedAc, computedAc, tolerance: 1E-12);
			}
		}

		[Fact]
		public static void TestBuildOperators_Poisson12Elements()
		{
			int numLevelsRequested = 5;
			double socTheta = 0.0;
			double orthonormalizationTolerance = 1E-10;
			double smoothingDampingFactor = 4.0 / 3.0;
			int smoothingDegree = 1;
			string relaxationStrategy = "SGS-4";

			CsrMatrix csrA = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);

			var terminationCriterion = new SystemSizeBasedTermination(10);
			var socStrategy = new SymmetricStrengthOfConnection(socTheta);
			var aggregationStrategy = new StandardAggregation();
			var candidateSelectionStrategy = new ConstantCandidateSelection();
			var candidateFittingStrategy = new ScalarCandidatesFitting(orthonormalizationTolerance);
			IJacobiWeightingStrategy weightingStrategy = new SpectralRadiusJacobiWeighting(dampingFactor: 4.0 / 3.0, new SystemRandomRng(seed: 13));
			var prolongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, smoothingDegree);
			var restrictionStrategy = new UnscaledTransposeRestriction();

			var relaxation = new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4);
			var candidateRelaxationStrategies = new List<INearNullspaceCandidatesRelaxationStrategy>();
			for (int i = 0; i < numLevelsRequested - 1; i++)
			{
				candidateRelaxationStrategies.Add(relaxation.CopyWithInitialSettings());
			}

			var hierarchy = new SaAmgOperatorHierarchy(numLevelsRequested, terminationCriterion, socStrategy, aggregationStrategy, candidateSelectionStrategy, candidateRelaxationStrategies, candidateFittingStrategy, prolongationSmoothing, restrictionStrategy, discardIntermediateData: false);
			hierarchy.BuildOperators(csrA);

			int expectedNumLevels = Poisson1D_12Elements_SaAmgResults.NumLevels;
			CsrMatrix expectedA0 = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(0);
			CsrMatrix expectedA1 = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(1);
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);
			var expectedAggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
			CsrMatrix expectedC0 = MatrixUtilities.ArrayToCsr(Poisson1D_12Elements_SaAmgResults.GetStrengthOfConnectionMatrix(0, socTheta));
			Matrix expectedB0 = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(0, relaxationStrategy));
			Matrix expectedB1 = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(1, relaxationStrategy));
			CsrMatrix expectedPt0 = Poisson1D_12Elements_SaAmgResults.GetTentativeProlongation(0, relaxationStrategy);
			CsrMatrix expectedP0 = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(0, weightingStrategy:  "SpectralRadius", smoothingDampingFactor, smoothingDegree);
			CsrMatrix expectedR0 = Poisson1D_12Elements_SaAmgResults.GetRestrictionMatrix(0);

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
			Assert.Equal(expectedAggregates.AggregatesOfGraphNodes, computedAggregates.AggregatesOfGraphNodes);
			Assert.Equal(expectedAggregates.RootGraphNodesOfAggregates, computedAggregates.RootGraphNodesOfAggregates);
			AssertUtilities.AssertEqualCsr(expectedC0, computedC0, tolerance: 1E-30);
			comparer.AssertEqual(expectedB0, computedB0);
			comparer.AssertEqual(expectedB1, computedB1);
			AssertUtilities.AssertEqualCsr(expectedPt0, computedPt0, tolerance: 1E-30);
			AssertUtilities.AssertEqualCsr(expectedP0, computedP0, tolerance: 1E-14);
			AssertUtilities.AssertEqualCsr(expectedR0, computedR0, tolerance: 1E-14);
		}
	}
}
