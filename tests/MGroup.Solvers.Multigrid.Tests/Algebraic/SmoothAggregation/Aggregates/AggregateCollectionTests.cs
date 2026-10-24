namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class AggregateCollectionTests
	{
		[Fact]
		public static void TestBuildAggregateMembershipMatrix_Cantilever8x4Elements()
		{
			// Level 0
			(int[] aggregatesOfGraphNodes0, int[] rootGraphNodesOfAggregates0, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 0);
			var aggregates0 = new AggregateCollection(aggregatesOfGraphNodes0, rootGraphNodesOfAggregates0);
			CsrMatrix aggOp0 = aggregates0.BuildAggregateMembershipMatrix();

			CsrMatrix aggOp0Expected = Cantilever2D_8x4Elements_SaAmgResults.GetAggregateMembershipMatrix(level: 0);
			AssertUtilities.AssertEqualCsr(aggOp0Expected, aggOp0);

			// Level 1
			(int[] aggregatesOfGraphNodes1, int[] rootGraphNodesOfAggregates1, _) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 1);
			var aggregates1 = new AggregateCollection(aggregatesOfGraphNodes1, rootGraphNodesOfAggregates1);
			CsrMatrix aggOp1 = aggregates1.BuildAggregateMembershipMatrix();

			CsrMatrix aggOp1Expected = Cantilever2D_8x4Elements_SaAmgResults.GetAggregateMembershipMatrix(level: 1);
			AssertUtilities.AssertEqualCsr(aggOp1Expected, aggOp1);
		}

		[Fact]
		public static void TestBuildAggregateMembershipMatrix_Poisson12Elements()
		{
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);
			var aggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
			CsrMatrix aggOp = aggregates.BuildAggregateMembershipMatrix();

			double[] valuesExpected = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
			int[] colIndicesExpected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] rowOffsetsExpected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

			AssertUtilities.AssertEqualCsr(4, valuesExpected, colIndicesExpected, rowOffsetsExpected, aggOp);
		}

		[Fact]
		public static void TestGatherGraphNodesPerAggregate_Cantilever8x4Elements()
		{
			// Level 0
			(int[] aggregatesOfGraphNodes0, int[] rootGraphNodesOfAggregates0, int[][] expectedGraphNodesPerAggregate0) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 0);

			var aggregates0 = new AggregateCollection(aggregatesOfGraphNodes0, rootGraphNodesOfAggregates0);
			int[][] graphNodesPerAggregate0 = aggregates0.GatherGraphNodesPerAggregate();

			Assert.Equal(expectedGraphNodesPerAggregate0, graphNodesPerAggregate0);

			// Level 1
			(int[] aggregatesOfGraphNodes1, int[] rootGraphNodesOfAggregates1, int[][] expectedGraphNodesPerAggregate1) = Cantilever2D_8x4Elements_SaAmgResults.GetAggregates(level: 1);

			var aggregates1 = new AggregateCollection(aggregatesOfGraphNodes1, rootGraphNodesOfAggregates1);
			int[][] graphNodesPerAggregate1 = aggregates1.GatherGraphNodesPerAggregate();

			Assert.Equal(expectedGraphNodesPerAggregate1, graphNodesPerAggregate1);
		}

		[Fact]
		public static void TestGatherGraphNodesPerAggregate_Poisson12Elements()
		{
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, int[][] expectedGraphNodesPerAggregate) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);
			
			var aggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
			int[][] graphNodesPerAggregate = aggregates.GatherGraphNodesPerAggregate();

			Assert.Equal(expectedGraphNodesPerAggregate, graphNodesPerAggregate);
		}
	}
}
