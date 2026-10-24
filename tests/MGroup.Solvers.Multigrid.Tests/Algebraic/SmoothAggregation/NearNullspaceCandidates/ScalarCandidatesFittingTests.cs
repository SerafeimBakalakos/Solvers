namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class ScalarCandidatesFittingTests
	{
		[Theory]
		[InlineData("None")]
		[InlineData("SGS-4")]
		public static void TestGetTentativeProlongation_Poisson12Elements(string relaxation)
		{
			int numDofsFine = Poisson1D_12Elements_SaAmgResults.SystemOrders[0];
			int numAggregates = Poisson1D_12Elements_SaAmgResults.SystemOrders[1];
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);
			var aggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);

			Assert.Equal(numDofsFine, aggregates.AggregatesOfGraphNodes.Length);
			Assert.Equal(numAggregates, aggregates.NumAggregates);

			var fineCandidates = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxation));
			var candidateFitting = new ScalarCandidatesFitting(orthonormalizationTolerance: 1E-10);
			(CsrMatrix tentativeProlongation, Matrix coarseCandidates, int blockSizeCoarse) = candidateFitting.FitCandidates(aggregates, fineCandidates);

			Assert.Equal(1, blockSizeCoarse);
			CsrMatrix expectedPt = Poisson1D_12Elements_SaAmgResults.GetTentativeProlongation(level: 0, relaxation);
			AssertUtilities.AssertEqualCsr(expectedPt, tentativeProlongation, tolerance: 1E-20);

			var expectedCoarseCandidates = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 1, relaxation));
			var matrixComparer = new MatrixComparer(tolerance: 1E-20);
			matrixComparer.AssertEqual(expectedCoarseCandidates, coarseCandidates);
		}
	}
}
