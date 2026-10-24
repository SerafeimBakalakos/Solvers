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
		public static void TestBuildAggregateMembershipMatrix()
		{
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, _) = Poisson1D12ElementsSaAmgResults.GetAggregates(level: 0);
			var aggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
			CsrMatrix am = aggregates.BuildAggregateMembershipMatrix();

			double[] valuesExpected = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
			int[] colIndicesExpected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] rowOffsetsExpected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

			AssertUtilities.AssertEqualCsr(4, valuesExpected, colIndicesExpected, rowOffsetsExpected, am);
		}

		[Fact]
		public static void TestGatherFineDofsPerAggregate()
		{
			(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates, int[][] expectedGraphNodesPerAggregate) = Poisson1D12ElementsSaAmgResults.GetAggregates(level: 0);
			
			var aggregates = new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregates);
			int[][] graphNodesPerAggregate = aggregates.GatherGraphNodesPerAggregate();

			Assert.Equal(expectedGraphNodesPerAggregate, graphNodesPerAggregate);
		}
	}
}
