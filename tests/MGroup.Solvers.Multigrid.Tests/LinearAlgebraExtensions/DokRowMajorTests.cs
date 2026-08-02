namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using CSparse;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

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
	}
}
