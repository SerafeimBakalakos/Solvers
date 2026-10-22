namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.TestData;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class CsrMatrixTestsExtensions
	{
		[Fact]
		public static void TestCalcAbsRowSums()
		{
			var matrix = CsrMatrix.CreateFromArrays(numRows: 8, numCols: 5,
				values: new double[]
				{
					2.0, -3.0, 4.0,
					-1.0, 5.0,
					7.0, -2.0,
					-4.0, 1.0, -6.0,
					3.0,
					-2.0, -8.0,
					9.0, -1.0
				},
				colIndices: new int[]
				{
					0, 2, 4,
					1, 3,
					0, 4,
					0, 2, 3,
					2,
					1, 4,
					0, 3
				},
				rowOffsets: new int[]
				{
					0, 3, 5, 7, 10, 10, 11, 13, 15
				},
				checkInput: true);

			double[] expected = new double[]
			{
				9.0, 6.0, 9.0, 11.0, 0.0, 3.0, 10.0, 10.0
			};

			double[] computed = matrix.CalcAbsRowSums();

			var comparer = new MatrixComparer(tolerance: 1E-15);
			comparer.AssertEqual(expected, computed);
		}



		[Fact]
		public static void TestGetDiagonalAbs()
		{
			double[] values =
			[
				21.0, 1.0, 4.0,
				1.0, -22.0, 2.0, 1.0,
				2.0, 23.0, 1.0, 3.0, 1.0, 1.0,
				4.0, 1.0, -24.0, 2.0, 4.0,
				3.0, 2.0, -25.0, 5.0, 2.0, 1.0,
				1.0, 4.0, 5.0, 26.0, 2.0, 3.0,
				1.0, 2.0, 27.0, 3.0,
				1.0, 3.0, -28.0, 4.0,
				2.0, 2.0, 4.0, -29.0,
				1.0, 3.0, 2.0, 30.0
			];

			int[] colIndices = SparsePosDef10by10.CsrColIndices;
			int[] rowOffsets = SparsePosDef10by10.CsrRowOffsets;
			int n = SparsePosDef10by10.Order;
			var matrix = CsrMatrix.CreateFromArrays(n, n, values, colIndices, rowOffsets, checkInput: true);

			double[] diagonalAbs = matrix.GetDiagonalAbs();
			double[] diagonalAbsExpected = [21, 22, 23, 24, 25, 26, 27, 28, 29, 30];

			Assert.Equal(diagonalAbsExpected, diagonalAbs);
		}

		[Fact]

		public static void TestNormalizeMaxAbs()
		{
			// Arrange: 8x6 matrix
			//
			// [  2,  0, -4,  1,  0,  0 ]
			// [  0, -3,  6,  0,  0, -2 ]
			// [ -2,  4,  0, -8,  0,  0 ]
			// [  0,  0,  5, -1,  0,  2 ]
			// [ -7,  0,  0,  0,  3, -1 ]
			// [  0,  9, -3,  0,  0,  0 ]
			// [  4, -2,  0,  0,  6,  0 ]
			// [  0,  0, -5,  2, -10, 0 ]

			double[] values =
			{
				 2, -4,  1,
				-3,  6, -2,
				-2,  4, -8,
				 5, -1,  2,
				-7,  3, -1,
				 9, -3,
				 4, -2,  6,
				-5,  2, -10
			};

			int[] colIndices =
			{
				0, 2, 3,
				1, 2, 5,
				0, 1, 3,
				2, 3, 5,
				0, 4, 5,
				1, 2,
				0, 1, 4,
				2, 3, 4
			};

			int[] rowOffsets = { 0, 3, 6, 9, 12, 15, 17, 20, 23 };

			// Expected normalized values:
			//
			// Row 0: divide by 4
			// Row 1: divide by 6
			// Row 2: divide by 8
			// Row 3: divide by 5
			// Row 4: divide by 7
			// Row 5: divide by 9
			// Row 6: divide by 6
			// Row 7: divide by 10
			double[] expectedValues =
			{
				0.5, 1.0,  0.25,
				0.5,  1.0, 1.0 / 3.0,
				0.25, 0.5, 1.0,
				1.0, 0.2,  0.4,
				1.0,  3.0 / 7.0, 1.0 / 7.0,
				1.0, 1.0 / 3.0,
				2.0 / 3.0, 1.0 / 3.0, 1.0,
				0.5,  0.2, 1.0
			};

			int[] expectedColIndices = (int[])colIndices.Clone();
			int[] expectedRowOffsets = (int[])rowOffsets.Clone();

			var matrix = CsrMatrix.CreateFromArrays(8, 6, values, colIndices, rowOffsets, checkInput: true);
			matrix.NormalizeMaxAbs();

			// Assert
			var comparer = new MatrixComparer(tolerance: 1E-30);
			comparer.AssertEqual(expectedValues, matrix.RawValues);
			Assert.Equal(expectedColIndices, matrix.RawColIndices);
			Assert.Equal(expectedRowOffsets, matrix.RawRowOffsets);
		}

		[Theory]
		[InlineData("square, aligned")]
		[InlineData("rectangular, misaligned")]
		public static void TestReduceByBlocksEmpty(string example)
		{
			CsrMatrix matrix;
			int[] blockSizes;
			CsrMatrix expectedReduced;
			if (example == "square, aligned")
			{
				(matrix, blockSizes, expectedReduced) = CreateSquareMatrixCase();
			}
			else if (example == "rectangular, misaligned")
			{
				(matrix, blockSizes, expectedReduced) = CreateSquareMatrixCase();
			}
			else
			{
				throw new NotImplementedException();
			}

			CsrMatrix reduced = matrix.ReduceByBlocksEmpty(rowBlockSize: blockSizes[0], colBlockSize: blockSizes[1]);
			expectedReduced.SetAllNonZeros(0.0);
			AssertUtilities.AssertEqualCsr(expectedReduced, reduced, tolerance: 1E-30);
		}

		[Theory]
		[InlineData("square, aligned")]
		[InlineData("rectangular, misaligned")]
		public static void TestReduceByBlocksFroberniusNorm(string example)
		{
			CsrMatrix matrix;
			int[] blockSizes;
			CsrMatrix expectedReduced;
			if (example == "square, aligned")
			{
				(matrix, blockSizes, expectedReduced) = CreateSquareMatrixCase();
			}
			else if (example == "rectangular, misaligned")
			{
				(matrix, blockSizes, expectedReduced) = CreateSquareMatrixCase();
			}
			else
			{
				throw new NotImplementedException();
			}

			CsrMatrix reduced = matrix.ReduceByBlocksFroberniusNorm(rowBlockSize: blockSizes[0], colBlockSize: blockSizes[1]);
			AssertUtilities.AssertEqualCsr(expectedReduced, reduced, tolerance: 1E-30);
		}

		[Fact]
		public static void TestScaleRows()
		{
			int n = Poisson1D_12Elements.SystemOrder;
			int[] colIndices = Poisson1D_12Elements.SystemMatrixCsrColIndices;
			int[] rowOffsets = Poisson1D_12Elements.SystemMatrixCsrRowOffsets;
			var matrix = CsrMatrix.CreateFromArrays(n, n, Poisson1D_12Elements.SystemMatrixCsrValues, colIndices, rowOffsets, checkInput: true);
			double[] diagonal = Poisson1D_12Elements.SystemMatrixDiagonal;

			matrix.ScaleRows(diagonal, invertDiagonal: true);
			double[] scaledValuesExpected = Poisson1D_12Elements.SystemMatrixNormalizedCsrValues;

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(scaledValuesExpected, matrix.RawValues);
		}

		// Test case 1: 12 x 12 matrix with 3 x 3 blocks. The result is a 4 x 4 matrix.
		private static (CsrMatrix Input, int[] blockSizes, CsrMatrix Expected) CreateSquareMatrixCase()
		{
			double[] values =
			{
				 3, -4,  // Row 0: columns 0, 7
				-4,  3,  // Row 1: columns 1, 8
				12,      // Row 2: column 2
				 5,      // Row 3: column 3
			   -12,      // Row 4: column 4
				-6,      // Row 6: column 0
				 8,      // Row 7: column 1
				 2,      // Row 8: column 9
				-3,      // Row 9: column 10
				 4,      // Row 10: column 11
			   -12       // Row 11: column 4
			};

			int[] colIndices =
			{
				0, 7,
				1, 8,
				2,
				3,
				4,
				0,
				1,
				9,
				10,
				11,
				4
			};

			int[] rowOffsets =
			{
				0,  2,  4,  5,
				6,  7,  7,  8,
				9, 10, 11, 12,
				13
			};

			var input = CsrMatrix.CreateFromArrays(12, 12, values, colIndices, rowOffsets, checkInput: true);

			// Nonempty blocks: (0,0) = 13, (0,2) = 5, (1,1) = 13, (2,0) = 10, (2,3) = 2, (3,1) = 12, (3,3) = 5.
			double[] expectedValues =
			{
				13, 5,
				13,
				10, 2,
				12, 5
			};

			int[] expectedColIndices =
			{
				0, 2,
				1,
				0, 3,
				1, 3
			};

			int[] expectedRowOffsets =
			{
				0, 2, 3, 5, 7
			};

			var expected = CsrMatrix.CreateFromArrays(4, 4, expectedValues, expectedColIndices, expectedRowOffsets, checkInput: true);

			return (input, [3, 3], expected);
		}

		// Test case 2: 14 x 17 matrix with 6 x 4 blocks. The result is a 3 x 5 matrix.
		private static (CsrMatrix Input, CsrMatrix Expected) CreateRectangularMatrixCase()
		{

			double[] values =
			{
				 3, -4,  12, // Row 0: columns 0, 5, 16
				-4,  3,  -5, // Row 1: columns 1, 6, 16
				12,           // Row 2: column 2
				 5,           // Row 6: column 3
			   -12,           // Row 7: column 4
				-6,           // Row 9: column 12
				 8,           // Row 10: column 13
				-3,           // Row 12: column 8
				 4            // Row 13: column 9
			};

			int[] colIndices =
			{
				0, 5, 16,
				1, 6, 16,
				2,
				3,
				4,
				12,
				13,
				8,
				9
			};

			int[] rowOffsets =
			{
				0,  3,  6,  7,  7,  7,  7,
				8,  9,  9, 10, 11, 11, 12, 13
			};

			var input = CsrMatrix.CreateFromArrays(14, 17, values, colIndices, rowOffsets, checkInput: true);

			// Hand-calculated block norms:
			// Block (0,0): sqrt(3^2 + (-4)^2 + 12^2) = 13.
			// Block (0,1): sqrt((-4)^2 + 3^2) = 5.
			// Block (0,4): sqrt(12^2 + (-5)^2) = 13.
			// Block (1,0): sqrt(5^2 + (-12)^2) = 13.
			// Block (1,3): sqrt((-6)^2 + 8^2) = 10.
			// Block (2,2): sqrt((-3)^2 + 4^2) = 5.

			double[] expectedValues =
			{
				13, 5, 13,
				13, 10,
				5
			};

			int[] expectedColIndices =
			{
				0, 1, 4,
				0, 3,
				2
			};

			int[] expectedRowOffsets =
			{
				0, 3, 5, 6
			};

			var expected = CsrMatrix.CreateFromArrays(3, 5, expectedValues, expectedColIndices, expectedRowOffsets, checkInput: true);

			return (input, expected);
		}
	}
}
