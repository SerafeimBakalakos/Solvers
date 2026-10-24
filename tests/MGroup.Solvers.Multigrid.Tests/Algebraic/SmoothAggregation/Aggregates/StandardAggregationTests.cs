namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class StandardAggregationTests
	{
		[Fact]
		public static void TestFindAggregates_Cantilever8x4Elements()
		{
			var aggregationStrategy = new StandardAggregation();

			// Level 0
			CsrMatrix C0 = Cantilever2D_8x4Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 0);
			AggregateCollection aggregates0 = aggregationStrategy.FindAggregates(C0);

			(int[] aggregatesOfGraphNodesExpected0, int[] rootGraphNodesOfAggregatesExpected0, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 0);
			Assert.Equal(rootGraphNodesOfAggregatesExpected0.Length, aggregates0.NumAggregates);
			Assert.Equal(aggregatesOfGraphNodesExpected0, aggregates0.AggregatesOfGraphNodes);
			Assert.Equal(rootGraphNodesOfAggregatesExpected0, aggregates0.RootGraphNodesOfAggregates);

			// Level 1
			CsrMatrix C1 = Cantilever2D_8x4Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 1);
			AggregateCollection aggregates1 = aggregationStrategy.FindAggregates(C1);

			(int[] aggregatesOfGraphNodesExpected1, int[] rootGraphNodesOfAggregatesExpected1, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 1);
			Assert.Equal(rootGraphNodesOfAggregatesExpected1.Length, aggregates1.NumAggregates);
			Assert.Equal(aggregatesOfGraphNodesExpected1, aggregates1.AggregatesOfGraphNodes);
			Assert.Equal(rootGraphNodesOfAggregatesExpected1, aggregates1.RootGraphNodesOfAggregates);
		}

		[Fact]
		public static void TestFindAggregates_Poisson12Elements()
		{
			double[,] C = Poisson1D_12Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 0, theta: 0);
			CsrMatrix soc = MatrixUtilities.ArrayToCsr(C);

			var aggregationStrategy = new StandardAggregation();
			AggregateCollection aggregates = aggregationStrategy.FindAggregates(soc);

			(int[] aggregatesOfGraphNodesExpected, int[] rootGraphNodesOfAggregatesExpected, _) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);
			int numAggregatesExpected = rootGraphNodesOfAggregatesExpected.Length;

			Assert.Equal(numAggregatesExpected, aggregates.NumAggregates);
			Assert.Equal(aggregatesOfGraphNodesExpected, aggregates.AggregatesOfGraphNodes);
			Assert.Equal(rootGraphNodesOfAggregatesExpected, aggregates.RootGraphNodesOfAggregates);
		}
	}
}
