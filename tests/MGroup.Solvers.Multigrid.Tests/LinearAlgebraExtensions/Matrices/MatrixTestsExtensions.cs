namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class MatrixTestsExtensions
	{
		[Fact]
		public static void TestGetSubmatrixRows()
		{
			var comparer = new MatrixComparer(tolerance: 1E-20);
			var matrixArray = new double[,]
			{
				{  1,  2,  3,  4,  5,  6,  7 },
				{ 11, 12, 13, 14, 15, 16, 17 },
				{ 21, 22, 23, 24, 25, 26, 27 },
				{ 31, 32, 33, 34, 35, 36, 37 },
				{ 41, 42, 43, 44, 45, 46, 47 }
			};
			var matrix = Matrix.CreateFromArray(matrixArray);


			// Case 1: Contiguous rows.
			var expectedContiguous = Matrix.CreateFromArray(new double[,]
			{
				{ 11, 12, 13, 14, 15, 16, 17 },
				{ 21, 22, 23, 24, 25, 26, 27 },
				{ 31, 32, 33, 34, 35, 36, 37 }
			});

			Matrix computedContiguous = matrix.GetSubmatrixRows(new[] { 1, 2, 3 });
			comparer.AssertEqual(expectedContiguous, computedContiguous);


			// Case 2: Jumbled row indices.
			// The output should preserve the order of the specified indices.
			var expectedJumbled = Matrix.CreateFromArray(new double[,]
			{
				{ 41, 42, 43, 44, 45, 46, 47 },
				{ 11, 12, 13, 14, 15, 16, 17 },
				{ 31, 32, 33, 34, 35, 36, 37 }
			});

			Matrix computedJumbled = matrix.GetSubmatrixRows(new[] { 4, 1, 3 });
			comparer.AssertEqual(expectedJumbled, computedJumbled);

			// Case 3: Negative row index.
			Assert.Throws<IndexOutOfRangeException>(
				() => matrix.GetSubmatrixRows(new[] { 0, -1, 2 }));

			// Case 4: Row index equal to NumRows (out of bounds).
			Assert.Throws<IndexOutOfRangeException>(
				() => matrix.GetSubmatrixRows(new[] { 0, 2, 5 }));
		}

		[Fact]
		public static void TestSetSubmatrix()
		{
			var comparer = new MatrixComparer(tolerance: 1E-20);
			var matrixArray = new double[,]
			{
				{  1,  2,  3,  4,  5,  6,  7 },
				{ 11, 12, 13, 14, 15, 16, 17 },
				{ 21, 22, 23, 24, 25, 26, 27 },
				{ 31, 32, 33, 34, 35, 36, 37 },
				{ 41, 42, 43, 44, 45, 46, 47 }
			};

			// Case 1: The submatrix fits inside the matrix at the specified offsets.
			var matrix = Matrix.CreateFromArray(matrixArray);

			var submatrix = Matrix.CreateFromArray(new double[,]
			{
				{ 100, 200, 300 },
				{ 400, 500, 600 }
			});

			// Place the 2 x 3 submatrix starting at row 1, column 2.
			matrix.SetSubmatrix(1, 2, submatrix);

			var expected = Matrix.CreateFromArray(new double[,]
			{
				{  1,  2,   3,   4,   5,  6,  7 },
				{ 11, 12, 100, 200, 300, 16, 17 },
				{ 21, 22, 400, 500, 600, 26, 27 },
				{ 31, 32,  33,  34,  35, 36, 37 },
				{ 41, 42,  43,  44,  45, 46, 47 }
			});

			comparer.AssertEqual(expected, matrix);


			// Case 2: The submatrix does not fit vertically.
			var matrixForInvalidCase = Matrix.CreateFromArray(matrixArray);

			var oversizedSubmatrix = Matrix.CreateFromArray(new double[,]
			{
				{ 100, 200, 300 },
				{ 400, 500, 600 }
			});

			// Starting at row 4, a 2-row submatrix would exceed NumRows.
			Assert.Throws<NonMatchingDimensionsException>(
				() => matrixForInvalidCase.SetSubmatrix(4, 1, oversizedSubmatrix));

			// Case 3: The submatrix does not fit horizontally.
			Assert.Throws<NonMatchingDimensionsException>(
				() => matrixForInvalidCase.SetSubmatrix(1, 5, oversizedSubmatrix));
		}
	}
}
