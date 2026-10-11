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
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class AggregateCollectionTests
	{
		[Fact]
		public static void TestBuildAggregateMembershipMatrix()
		{
			int[] aggregatesOfFineDofs = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] fineDofRootsOfAggregates = [0, 3, 6, 9];
			var aggregates = new AggregateCollection(aggregatesOfFineDofs, fineDofRootsOfAggregates);
			CsrMatrix am = aggregates.BuildAggregateMembershipMatrix();

			double[] valuesExpected = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
			int[] colIndicesExpected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] rowOffsetsExpected = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

			AssertUtilities.AssertEqualCsr(4, valuesExpected, colIndicesExpected, rowOffsetsExpected, am);
		}

		[Fact]
		public static void TestGatherFineDofsPerAggregate()
		{
			int[] aggregatesOfFineDofs = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] fineDofRootsOfAggregates = [0, 3, 6, 9];
			var aggregates = new AggregateCollection(aggregatesOfFineDofs, fineDofRootsOfAggregates);

			int[][] fineDofsPerAggregate = aggregates.GatherFineDofsPerAggregate();
			int[][] fineDofsPerAggregateExpected = new int[][]
			{
				new int[] { 0, 1 },
				new int[] { 2, 3, 4 },
				new int[] { 5, 6, 7 },
				new int[] { 8, 9, 10 }
			};

			Assert.Equal(fineDofsPerAggregateExpected, fineDofsPerAggregate);
		}
	}
}
