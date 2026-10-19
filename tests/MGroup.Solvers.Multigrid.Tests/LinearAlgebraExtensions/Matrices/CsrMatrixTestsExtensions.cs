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
	}
}
