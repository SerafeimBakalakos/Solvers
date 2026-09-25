namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;

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

			Assert.Equal(valuesExpected, am.RawValues);
			Assert.Equal(colIndicesExpected, am.RawColIndices);
			Assert.Equal(rowOffsetsExpected, am.RawRowOffsets);
		}
	}
}
