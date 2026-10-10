namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class CsrCsrBinaryOperationsTests
	{
		private const int numRows = 6;
		private const int numColumns = 7;

		// A =
		// [ 1  0*  0  2  0  0  0
		//   0  3   0  0  0  0  4
		//   0  0   5  0  0  6  0
		//   0  0   0  0* 7  0  0
		//   8  0   0  0  9  0  0
		//   0  0   0 10  0  0 11 ]

		// A with explicit zeros.
		private static readonly double[] AValues =
		{
			1, 0, 2,
			3, 4,
			5, 6,
			0, 7,
			8, 9,
			10, 11
		};

		private static readonly int[] AColumnIndices =
		{
			0, 1, 3,
			1, 6,
			2, 5,
			3, 4,
			0, 4,
			3, 6
		};

		private static readonly int[] ARowOffsets =
		{
			0, 3, 5, 7, 9, 11, 13
		};

		// A without explicit zeros.
		private static readonly double[] ACanonicalValues =
		{
			1, 2,
			3, 4,
			5, 6,
			7,
			8, 9,
			10, 11
		};

		private static readonly int[] ACanonicalColumnIndices =
		{
			0, 3,
			1, 6,
			2, 5,
			4,
			0, 4,
			3, 6
		};

		private static readonly int[] ACanonicalRowOffsets =
		{
			0, 2, 4, 6, 7, 9, 11
		};


		// B =
		// [ 0  0  1  2  0  0  0
		//   0* 3  0  0  0  2  4
		//   0  0  2  0  0  0 12
		//   0  0  0  0* 7  0  1
		//   0  1  0  0  9  0  0
		//   0  0  0 10  0  5 11 ]

		// B with explicit zeros.
		private static readonly double[] BValues =
		{
			1, 2,
			0, 3, 2, 4,
			2, 12,
			0, 7, 1,
			1, 9,
			10, 5, 11
		};

		private static readonly int[] BColumnIndices =
		{
			2, 3,
			0, 1, 5, 6,
			2, 6,
			3, 4, 6,
			1, 4,
			3, 5, 6
		};

		private static readonly int[] BRowOffsets =
		{
			0, 2, 6, 8, 11, 13, 16
		};

		// B without explicit zeros.
		private static readonly double[] BCanonicalValues =
		{
			1, 2,
			3, 2, 4,
			2, 12,
			7, 1,
			1, 9,
			10, 5, 11
		};

		private static readonly int[] BCanonicalColumnIndices =
		{
			2, 3,
			1, 5, 6,
			2, 6,
			4, 6,
			1, 4,
			3, 5, 6
		};

		private static readonly int[] BCanonicalRowOffsets =
		{
			0, 2, 5, 7, 9, 11, 14
		};


		// C = A - B =
		// [  1   0*  -1   0*   0   0   0
		//    0*  0*   0   0    0  -2   0*
		//    0   0    3   0    0   6 -12
		//    0   0    0   0*   0*  0  -1
		//    8  -1    0   0    0*  0   0
		//    0   0    0   0*   0  -5   0* ]
		//
		// The zeros marked by the test data as cancellation zeros are:
		// (0,3), (1,1), (1,6), (3,3), (3,4), (4,4), (5,3), (5,6).
		//
		// There are also zeros inherited from explicit zero entries in A/B.

		// C with all entries retained, including zeros.
		private static readonly double[] CValues =
		{
			1, 0, -1, 0,
			0, 0, -2, 0,
			3, 6, -12,
			0, 0, -1,
			8, -1, 0,
			0, -5, 0
		};

		private static readonly int[] CColumnIndices =
		{
			0, 1, 2, 3,
			0, 1, 5, 6,
			2, 5, 6,
			3, 4, 6,
			0, 1, 4,
			3, 5, 6
		};

		private static readonly int[] CRowOffsets =
		{
			0, 4, 8, 11, 14, 17, 20
		};

		// C without zeros.
		private static readonly double[] CCanonicalValues =
		{
			1, -1,
			-2,
			3, 6, -12,
			-1,
			8, -1,
			-5
		};

		private static readonly int[] CCanonicalColumnIndices =
		{
			0, 2,
			5,
			2, 5, 6,
			6,
			0, 1,
			5
		};

		private static readonly int[] CCanonicalRowOffsets =
		{
			0, 2, 3, 6, 7, 9, 10
		};


		[Fact]
		public static void TestCsrMinusCsr()
		{
			CsrMatrix matrixA = CsrMatrix.CreateFromArrays(numRows, numColumns, AValues, AColumnIndices, ARowOffsets, checkInput: true);
			CsrMatrix matrixB = CsrMatrix.CreateFromArrays(numRows, numColumns, BValues, BColumnIndices, BRowOffsets, checkInput: true);
			CsrMatrix expectedC = CsrMatrix.CreateFromArrays(numRows, numColumns, CValues, CColumnIndices, CRowOffsets, checkInput: true);
			
			CsrMatrix computedC = CsrCsrBinaryOperations.CsrMinusCsr(matrixA, matrixB);
			AssertUtilities.AssertEqualCsr(expectedC, computedC);
		}


		[Fact]
		public static void TestCsrMinusCsrCanonical()
		{
			CsrMatrix matrixA = CsrMatrix.CreateFromArrays(numRows, numColumns, ACanonicalValues, ACanonicalColumnIndices, ACanonicalRowOffsets, checkInput: true);
			CsrMatrix matrixB = CsrMatrix.CreateFromArrays(numRows, numColumns, BCanonicalValues, BCanonicalColumnIndices, BCanonicalRowOffsets, checkInput: true);
			CsrMatrix expectedC = CsrMatrix.CreateFromArrays(numRows, numColumns, CCanonicalValues, CCanonicalColumnIndices, CCanonicalRowOffsets, checkInput: true);

			CsrMatrix computedC = CsrCsrBinaryOperations.CsrMinusCsrCanonical(matrixA, matrixB);
			AssertUtilities.AssertEqualCsr(expectedC, computedC);
		}


		[Fact]
		public static void TestCsrMinusCsrCanonicalResult()
		{
			CsrMatrix matrixA = CsrMatrix.CreateFromArrays(numRows, numColumns, AValues, AColumnIndices, ARowOffsets, checkInput: true);
			CsrMatrix matrixB = CsrMatrix.CreateFromArrays(numRows, numColumns, BValues, BColumnIndices, BRowOffsets, checkInput: true);
			CsrMatrix expectedC = CsrMatrix.CreateFromArrays(numRows, numColumns, CCanonicalValues, CCanonicalColumnIndices, CCanonicalRowOffsets, checkInput: true);

			CsrMatrix computedC = CsrCsrBinaryOperations.CsrMinusCsrCanonicalResult(matrixA, matrixB);
			AssertUtilities.AssertEqualCsr(expectedC, computedC);
		}
	}
}
