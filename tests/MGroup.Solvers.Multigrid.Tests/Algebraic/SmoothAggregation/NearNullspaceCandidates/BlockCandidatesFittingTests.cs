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

	public static class BlockCandidatesFittingTests
	{
		[Fact]
		public static void TestGetTentativeProlongation_Cantilever8x4Elements()
		{
			var candidateFitting = new BlockCandidatesFitting(orthonormalizationTolerance: 1E-10);

			// Level 0
			AggregateCollection aggregates0 = GetAggregates(level: 0);
			var B0 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(level: 0));
			(CsrMatrix T0, Matrix Bc0, int blockSizeCoarse0) = candidateFitting.FitCandidates(aggregates0, B0);

			Assert.Equal(3, blockSizeCoarse0);
			CsrMatrix expectedT0 = Cantilever2D_8x4Elements_SaAmgResults.GetTentativeProlongation(level: 0);
			AssertUtilities.AssertEqualCsr(expectedT0, T0, tolerance: 1E-12);

			var expectedBc0 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(level: 1));
			var comparer0 = new MatrixComparer(tolerance: 1E-15);
			comparer0.AssertEqual(expectedBc0, Bc0);

			// Level 1
			AggregateCollection aggregates1 = GetAggregates(level: 1);
			var B1 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(level: 1));
			(CsrMatrix T1, Matrix Bc1, int blockSizeCoarse1) = candidateFitting.FitCandidates(aggregates1, B1);

			Assert.Equal(3, blockSizeCoarse1);
			CsrMatrix expectedT1 = Cantilever2D_8x4Elements_SaAmgResults.GetTentativeProlongation(level: 1);
			AssertUtilities.AssertEqualCsr(expectedT1, T1, tolerance: 1E-14);

			var expectedBc1 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(level: 2));
			var comparer1 = new MatrixComparer(tolerance: 1E-15);
			comparer1.AssertEqual(expectedBc1, Bc1);
		}

		private static AggregateCollection GetAggregates(int level)
		{
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level);
			return new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
		}
	}
}
