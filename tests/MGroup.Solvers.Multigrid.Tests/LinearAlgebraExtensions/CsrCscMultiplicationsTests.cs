namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	using Xunit;

	public static class CsrCscMultiplicationsTests
	{
		[Fact]
		public static void TestCsrTimesCscToCsc()
		{
			// A (6 x 8):
			// [ 2  0  0  3  0  0  0  0 ]
			// [ 0  0 -1  0  0  4  0  0 ]
			// [ 0  5  0  0  0  0  0  2 ]
			// [ 0  0  0  1  0  0  6  0 ]
			// [ 0  0  7  0  0  0  0  0 ]
			// [ 0  0  0  0  8  0  0  9 ]
			var matrixA = CsrMatrix.CreateFromArrays(numRows: 6, numCols: 8,
				values: new[]
				{
					2.0, 3.0,
					-1.0, 4.0,
					5.0, 2.0,
					1.0, 6.0,
					7.0,
					8.0, 9.0
				},
				colIndices: new[]
				{
					0, 3,
					2, 5,
					1, 7,
					3, 6,
					2,
					4, 7
				},
				rowOffsets: new[]
				{
					0, 2, 4, 6, 8, 9, 11
				},
				checkInput: false
			);

			// B (8 x 5):
			// [ 1   0    0    0       0 ]
			// [ 0   0    2    0       0 ]
			// [ 0   3    0    0       0 ]
			// [ 4   0    0    0       5 ]
			// [ 0   0    0    6       0 ]
			// [ 0   7    0    0       0 ]
			// [ 0   0    8    0       0 ]
			// [ 0   0    0   -16/3    0 ]
			var matrixB = CscMatrix.CreateFromArrays(numRows: 8, numCols: 5,
				values: new[]
				{
					1.0,
					4.0,
					3.0, 7.0,
					2.0, 8.0,
					6.0, -16.0 / 3.0,
					5.0
				},
				rowIndices: new[]
				{
					0,
					3,
					2, 5,
					1, 6,
					4, 7,
					3
				},
				colOffsets: new[]
				{
					0, 2, 4, 6, 8, 9
				},
				checkInput: false
			);

			// C = A * B (6 x 5):
			// [14   0    0    0   15]
			// [ 0  25    0    0    0]
			// [ 0   0   10  -32/3  0]
			// [ 4   0   48    0    5]
			// [ 0  21    0    0    0]
			// [ 0   0    0    0    0]  <-- structural match, but 8*6 + 9*(-16/3) = 0
			double[] valuesCExpected =
			{
				14.0, 4.0, 
				25.0, 21.0, 
				10.0, 48.0, 
				-32.0/3, 0.0, 
				15.0,5.0,
			};

			int[] rowIndicesCExpected =
			{
				0, 3,       // column 0
				1, 4,       // column 1
				2, 3,       // column 2
				2, 5,       // column 3
				0, 3        // column 4
			};

			int[] colOffsetsCExpected =
			{
				0, 2, 4, 6, 8, 10
			};

			CscMatrix matrixC = CsrCscMultiplications.CsrTimesCscToCsc(matrixA, matrixB);

			// Therefore we expect:
			// - 9 numerical non-zeros
			// - 10 stored entries, including the explicit zero at (5, 3)
			// C[5,3] = 8 * 6 + 9 * (-16/3) = 48 - 48 = 0 cancels out, but is explicitly stored.
			Assert.Equal(6, matrixC.NumRows);
			Assert.Equal(5, matrixC.NumColumns);
			Assert.Equal(10, matrixC.NumNonZeros);

			Assert.Equal(valuesCExpected, matrixC.RawValues);
			Assert.Equal(rowIndicesCExpected, matrixC.RawRowIndices);
			Assert.Equal(colOffsetsCExpected, matrixC.RawColOffsets);
		}

		[Fact]
		public static void TestCsrTimesCscToCsr()
		{
			// A (5 x 7):
			// [ 2  0  0  1  0  0  0 ]
			// [ 0  3  0  0  0  0  2 ]
			// [ 0  0 -2  0  3  0  0 ]
			// [ 5  0  0  0  0  2  0 ]
			// [ 0  0  0  7  0  0 -2 ]
			var matrixA = CsrMatrix.CreateFromArrays(numRows: 5, numCols: 7,
				values: new[]
				{
					2.0, 1.0,
					3.0, 2.0,
					-2.0, 3.0,
					5.0, 2.0,
					7.0, -2.0
				},
				colIndices: new[]
				{
					0, 3,
					1, 6,
					2, 4,
					0, 5,
					3, 6
				},
				rowOffsets: new[]
				{
					0, 2, 4, 6, 8, 10
				},
				checkInput: false
			);

			// B (7 x 4):
			// [ 1  0  0   2 ]
			// [ 0  4  0   0 ]
			// [ 2  0  3   0 ]
			// [ 0  0  0  -1 ]
			// [ 0  0  2   6 ]
			// [ 0  3  0   4 ]
			// [ 0  0  0   3 ]
			var matrixB = CscMatrix.CreateFromArrays(numRows: 7,numCols: 4,
				values: new[]
				{
					1.0, 2.0,
					4.0, 3.0,
					3.0, 2.0,
					2.0, -1.0, 6.0, 4.0, 3.0
				},
				rowIndices: new[]
				{
					0, 2,
					1, 5,
					2, 4,
					0, 3, 4, 5, 6
				},
				colOffsets: new[]
				{
					0, 2, 4, 6, 11
				},
				checkInput: false
			);

			// C = A * B (5 x 4):
			// [ 2   0   0   3 ]
			// [ 0  12   0   6 ]
			// [-4   0   0  18 ]  <-- C[2,2] is stored explicitly as zero
			// [ 5   6   0  18 ]
			// [ 0   0   0 -13 ]
			double[] valuesCExpected =
			{
				2.0, 3.0,
				12.0, 6.0,
				-4.0, 0.0, 18.0,
				5.0, 6.0, 18.0,
				-13.0
			};

			int[] colIndicesCExpected =
			{
				0, 3,
				1, 3,
				0, 2, 3,
				0, 1, 3,
				3
			};

			int[] rowOffsetsCExpected =
			{
				0, 2, 4, 7, 10, 11
			};

			CsrMatrix matrixC = CsrCscMultiplications.CsrTimesCscToCsr(matrixA, matrixB);

			// We expect are 11 stored entries:
			// 10 numerical non-zeros + 1 explicit zero.
			Assert.Equal(5, matrixC.NumRows);
			Assert.Equal(4, matrixC.NumColumns);
			Assert.Equal(11, matrixC.NumNonZeros);

			Assert.Equal(valuesCExpected, matrixC.RawValues);
			Assert.Equal(colIndicesCExpected, matrixC.RawColIndices);
			Assert.Equal(rowOffsetsCExpected, matrixC.RawRowOffsets);
		}
	}
}
