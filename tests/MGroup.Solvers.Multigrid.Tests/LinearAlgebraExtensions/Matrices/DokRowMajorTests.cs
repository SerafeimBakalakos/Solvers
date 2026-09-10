namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using CSparse;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public class DokRowMajorTests
	{
		[Fact]
		public static void TestCountNonZeros()
		{
			DokRowMajor matrix = Array2DToSparse(new double[,]
			{
				{ 0, 4, 0, 0, 0, 8 },
				{ 5, 0, 0, 0, 10, 0 },
				{ 0, 6, 0, 0, 0, 12 },
				{ 0, 0, 0, 12, 0, 0 },
				{ 0, 0, 15, 0, 0, 0 },
				{ 0, 0, 0, 18, 0, 0 }
			});
			Assert.Equal(matrix.CountNonZeros(), 9);
		}

		[Fact]
		public static void TestGetSubmatrix1()
		{
			DokRowMajor original = CreateDok(SparseRectangular10by5.Matrix);
			var rowsToKeep = new[] { 1, 4, 8 };
			var colsToKeep = new[] { 0, 2, 3, 4 };
			DokRowMajor submatrixComputed = original.GetSubmatrix(rowsToKeep, colsToKeep);

			var submatrixExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0.72602,   0.64231,   2.74205,   0.00000 },
				{ 2.44372,   0.00000,   2.98743,   0.00000 },
				{ 0.42069,   2.65133,   2.70976,   0.52623 }
			});
			int nnzExpected = 9;

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(submatrixExpected, submatrixComputed);
			Assert.Equal(nnzExpected, submatrixComputed.CountNonZeros());
		}

		[Fact]
		public static void TestGetSubmatrix2()
		{
			DokRowMajor original = CreateDok(SparseRectangular10by5.Matrix);

			// Both rows and columns are deliberately permuted.
			var rowsToKeep = new[] { 8, 2, 5, 0, 7 };
			var colsToKeep = new[] { 4, 1, 3 };
			DokRowMajor submatrixComputed = original.GetSubmatrix(rowsToKeep, colsToKeep);

			var submatrixExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0.52623,   0.00000,   2.70976 },
				{ 0.02756,   0.00000,   0.00000 },
				{ 0.00000,   0.00000,   3.53890 },
				{ 0.00000,   0.00000,   2.09065 },
				{ 3.85200,   0.00000,   0.00000 }
			});
			int nnzExpected = 6;

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(submatrixExpected, submatrixComputed);
			Assert.Equal(nnzExpected, submatrixComputed.CountNonZeros());
		}

		[Fact]
		public static void TestGetSubmatrix3()
		{
			DokRowMajor original = CreateDok(SparsePosDef10by10.Matrix);

			var rowsToKeep = new[] { 0, 2, 5, 9 };
			var colsToKeep = new[] { 1, 2, 4, 6, 8, 9 };
			DokRowMajor submatrixComputed = original.GetSubmatrix(rowsToKeep, colsToKeep);

			var submatrixExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 1.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
				{ 2.0, 23.0, 3.0, 0.0, 0.0, 0.0 },
				{ 0.0, 1.0, 5.0, 0.0, 2.0, 3.0 },
				{ 0.0, 0.0, 1.0, 0.0, 0.0, 30.0 }
			});
			int nnzExpected = 10;

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(submatrixExpected, submatrixComputed);
			Assert.Equal(nnzExpected, submatrixComputed.CountNonZeros());
		}

		[Fact]
		public static void TestGetSubmatrix4()
		{
			DokRowMajor original = CreateDok(SparsePosDef10by10.Matrix);

			// More rows than columns, and both dimensions are permuted.
			var rowsToKeep = new[] { 8, 3, 6, 1, 9, 4 };
			var colsToKeep = new[] { 7, 2, 5, 0 };

			DokRowMajor submatrixComputed = original.GetSubmatrix(rowsToKeep, colsToKeep);

			var submatrixExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 4.0, 0.0, 2.0, 0.0 },
				{ 0.0, 1.0, 4.0, 4.0 },
				{ 3.0, 0.0, 0.0, 0.0 },
				{ 0.0, 2.0, 0.0, 1.0 },
				{ 2.0, 0.0, 3.0, 0.0 },
				{ 0.0, 3.0, 5.0, 0.0 }
			});
			int nnzExpected = 12;

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(submatrixExpected, submatrixComputed);
			Assert.Equal(nnzExpected, submatrixComputed.CountNonZeros());
		}

		[Fact]
		public static void TestScaleIntoThis()
		{
			DokRowMajor matrix = Array2DToSparse(new double[,]
			{
				{ 0, 4, 0, 0, 0, 8 },
				{ 5, 0, 0, 0, 10, 0 },
				{ 0, 6, 0, 0, 0, 12 },
				{ 0, 0, 0, 12, 0, 0 },
				{ 0, 0, 15, 0, 0, 0 },
				{ 0, 0, 0, 18, 0, 0 }
			});
			Assert.Equal(matrix.CountNonZeros(), 9);

			var resultExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0, -2, 0, 0, 0, -4 },
				{ -2.5, 0, 0, 0, -5, 0 },
				{ 0, -3, 0, 0, 0, -6 },
				{ 0, 0, 0, -6, 0, 0 },
				{ 0, 0, -7.5, 0, 0, 0 },
				{ 0, 0, 0, -9, 0, 0 }
			});

			matrix.ScaleIntoThis(-0.5);
			Assert.True(matrix.Equals(resultExpected));
		}

		[Fact]
		public static void TestKroneckerProduct()
		{
			DokRowMajor matrixA = Array2DToSparse(new double[,]
			{
				{ 1, 0, 2 },
				{ 0, 3, 0 },
			});
			Assert.Equal(matrixA.CountNonZeros(), 3);

			DokRowMajor matrixB = Array2DToSparse(new double[,]
			{
				{ 0, 4 },
				{ 5, 0 },
				{ 0, 6 }
			});
			Assert.Equal(matrixB.CountNonZeros(), 3);

			var productExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0, 4, 0, 0, 0, 8 },
				{ 5, 0, 0, 0, 10, 0 },
				{ 0, 6, 0, 0, 0, 12 },
				{ 0, 0, 0, 12, 0, 0 },
				{ 0, 0, 15, 0, 0, 0 },
				{ 0, 0, 0, 18, 0, 0 }
			});

			DokRowMajor product = matrixA.KroneckerProduct(matrixB);

			Assert.True(product.Equals(productExpected));
        }

		[Fact]
		public static void TestKroneckerProductMatrixTimesIdentity()
		{
			DokRowMajor matrix = Array2DToSparse(new double[,]
			{
				{ 2, 0 },
				{ 0, 3 },
				{ 4, 5 },
			});
			Assert.Equal(matrix.CountNonZeros(), 4);

			var productExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 2, 0, 0, 0, 0, 0 },
				{ 0, 2, 0, 0, 0, 0 },
				{ 0, 0, 2, 0, 0, 0 },
				{ 0, 0, 0, 3, 0, 0 },
				{ 0, 0, 0, 0, 3, 0 },
				{ 0, 0, 0, 0, 0, 3 },
				{ 4, 0, 0, 5, 0, 0 },
				{ 0, 4, 0, 0, 5, 0 },
				{ 0, 0, 4, 0, 0, 5 },
			});

			DokRowMajor product = matrix.KroneckerProductThisTimesIdentity(3);
			Assert.True(product.Equals(productExpected));
		}

		[Fact]
		public static void TestKroneckerProductIdentityTimesMatrix()
		{
			DokRowMajor matrix = Array2DToSparse(new double[,]
			{
				{ 0, 7, 0 },
				{ 8, 0, 9 },
			});
			Assert.Equal(matrix.CountNonZeros(), 3);

			var productExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0, 7, 0, 0, 0, 0, 0, 0, 0 },
				{ 8, 0, 9, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 7, 0, 0, 0, 0 },
				{ 0, 0, 0, 8, 0, 9, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 7, 0 },
				{ 0, 0, 0, 0, 0, 0, 8, 0, 9 },
			});

			DokRowMajor product = matrix.KroneckerProductIdentityTimesThis(3);
			Assert.True(product.Equals(productExpected));
		}

		[Fact]
		public static void TestTranspose()
		{
			DokRowMajor matrix = Array2DToSparse(new double[,]
			{
				{ 0, 4, 0,  0,  0,  8, 9, 0 },
				{ 5, 0, 0,  0, 10,  0, 0, 3 },
				{ 0, 6, 0,  0,  0, 12, 2, 0 },
				{ 0, 0, 0, 12,  0,  0, 0, 7 },
				{ 0, 0, 15, 0,  0,  0, 4, 6 },
				{ 0, 0, 0, 18,  0,  0, 0, 0 }
			});
			Assert.Equal(matrix.CountNonZeros(), 15);

			var transposeExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 0,  5,  0,  0,  0,  0 },
				{ 4,  0,  6,  0,  0,  0 },
				{ 0,  0,  0,  0, 15,  0 },
				{ 0,  0,  0, 12,  0, 18 },
				{ 0, 10,  0,  0,  0,  0 },
				{ 8,  0, 12,  0,  0,  0 },
				{ 9,  0,  2,  0,  4,  0 },
				{ 0,  3,  0,  7,  6,  0 },
			});

			DokRowMajor transpose = matrix.Transpose();
			Assert.True(transpose.Equals(transposeExpected));
		}

		private static DokRowMajor Array2DToSparse(double[,] array)
		{
			int m = array.GetLength(0);
			int n = array.GetLength(1);
			var result = DokRowMajor.CreateEmpty(m, n);
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					if (array[i, j] != 0.0)
					{
						result[i, j] = array[i, j];
					}
				}
			}
			return result;
		}

		private static DokRowMajor CreateDok(double[,] matrix)
		{
			int m = matrix.GetLength(0);
			int n = matrix.GetLength(1);
			var dok = DokRowMajor.CreateEmpty(m, n);
			for (int i = 0; i < m; ++i)
			{
				for (int j = 0; j < n; ++j)
				{
					if (matrix[i, j] != 0.0) dok[i, j] = matrix[i, j];
				}
			}
			return dok;
		}
	}
}
