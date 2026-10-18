namespace MGroup.Solvers.Multigrid.Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Logging;
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

		public static void AssertEqualResidualHistories(double[] expectedResiduals, ResidualNormHistory computedHistory, double tolerance)
		{
			var comparer = new MatrixComparer(tolerance);
			if (computedHistory.IsInitialResidualStored)
			{
				double[] computedResiduals = computedHistory.GetAllResiduals();
				Assert.Equal(computedResiduals.Length - 1, computedHistory.NumSolverIterations);
				Assert.Equal(expectedResiduals.Length - 1, computedHistory.NumSolverIterations);
				comparer.AssertEqual(expectedResiduals, computedResiduals);
			}
			else
			{
				double[] computedResiduals = computedHistory.GetResidualsExceptInitial();
				Assert.Equal(computedResiduals.Length, computedHistory.NumSolverIterations);
				Assert.Equal(expectedResiduals.Length, computedHistory.NumSolverIterations);
				comparer.AssertEqual(expectedResiduals, computedResiduals);
			}
		}
	}
}
