namespace MGroup.Solvers.Multigrid.Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public class AssertUtilities
	{
		public static void AssertEqualCsr(int numColumnsExpected, double[] expectedValues, int[] expectedColIndices, int[] expectedRowOffsets, CsrMatrix computed, double? tolerance = null)
		{
			Assert.Equal(expectedRowOffsets.Length - 1, computed.NumRows);
			Assert.Equal(numColumnsExpected, computed.NumColumns);
			Assert.Equal(expectedRowOffsets, computed.RawRowOffsets);
			Assert.Equal(expectedColIndices, computed.RawColIndices);
			if (tolerance is double tol)
			{
				var comparer = new MatrixComparer(tol);
				comparer.AssertEqual(expectedValues, computed.RawValues);
			}
			else
			{
				Assert.Equal(expectedValues, computed.RawValues);
			}
		}

		public static void AssertEqualCsr(CsrMatrix expected, CsrMatrix computed, double? tolerance = null)
		{
			Assert.Equal(expected.NumRows, computed.NumRows);
			Assert.Equal(expected.NumColumns, computed.NumColumns);
			Assert.Equal(expected.RawRowOffsets, computed.RawRowOffsets);
			Assert.Equal(expected.RawColIndices, computed.RawColIndices);
			if (tolerance is double tol)
			{
				var comparer = new MatrixComparer(tol);
				comparer.AssertEqual(expected.RawValues, computed.RawValues);
			}
			else
			{
				Assert.Equal(expected.RawValues, computed.RawValues);
			}
		}
	}
}
