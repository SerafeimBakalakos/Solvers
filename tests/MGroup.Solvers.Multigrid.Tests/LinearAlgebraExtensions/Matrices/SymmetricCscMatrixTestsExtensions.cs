namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Newtonsoft.Json.Linq;

	using Xunit;

	public static class SymmetricCscMatrixTestsExtensions
	{
		[Fact]
		public static void TestPermuteRowsAndCols()
		{
			var matrixA = SymmetricCscMatrix.CreateFromArrays(
					SparsePosDef10by10.Order, SparsePosDef10by10.SymmetricCscValues,
					SparsePosDef10by10.SymmetricCscRowIndices, SparsePosDef10by10.SymmetricCscColOffsets,
					true);

			int[] inversePermutationArray = SparsePosDef10by10.MatlabPermutationAMD;
			var permutation = Permutation.CreateWithInversePermutation(inversePermutationArray);
			SymmetricCscMatrix matrixBComputed = matrixA.PermuteRowsAndCols(permutation);

			var matrixBExpected = Matrix.CreateFromArray(new double[,]
			{
				{ 21.0,  1.0,  0.0,  0.0,  0.0,  0.0,  4.0,  0.0,  0.0,  0.0 },
				{  1.0, 22.0,  0.0,  0.0,  0.0,  2.0,  0.0,  0.0,  0.0,  1.0 },
				{  0.0,  0.0, 29.0,  0.0,  4.0,  0.0,  0.0,  0.0,  2.0,  0.0 },
				{  0.0,  0.0,  0.0, 30.0,  2.0,  0.0,  0.0,  1.0,  3.0,  0.0 },
				{  0.0,  0.0,  4.0,  2.0, 28.0,  1.0,  0.0,  0.0,  0.0,  3.0 },
				{  0.0,  2.0,  0.0,  0.0,  1.0, 23.0,  1.0,  3.0,  1.0,  0.0 },
				{  4.0,  0.0,  0.0,  0.0,  0.0,  1.0, 24.0,  2.0,  4.0,  0.0 },
				{  0.0,  0.0,  0.0,  1.0,  0.0,  3.0,  2.0, 25.0,  5.0,  2.0 },
				{  0.0,  0.0,  2.0,  3.0,  0.0,  1.0,  4.0,  5.0, 26.0,  0.0 },
				{  0.0,  1.0,  0.0,  0.0,  3.0,  0.0,  0.0,  2.0,  0.0, 27.0 }
			});

			double[] valuesExpected = 
			{
				21.0,
				1.0, 22.0,
				29.0,
				30.0,
				4.0, 2.0, 28.0,
				2.0, 1.0, 23.0,
				4.0, 1.0, 24.0,
				1.0, 3.0, 2.0, 25.0,
				2.0, 3.0, 1.0, 4.0, 5.0, 26.0,
				1.0, 3.0, 2.0, 27.0
			};

			int[] rowIndicesExpected =
			{
				0,
				0, 1,
				2,
				3,
				2, 3, 4,
				1, 4, 5,
				0, 5, 6,
				3, 5, 6, 7,
				2, 3, 5, 6, 7, 8,
				1, 4, 7, 9
			};

			int[] colOffsetsExpected =
			{
				 0, 1, 3, 4, 5, 8, 11, 14, 18, 24, 28
			};

			double tol = 1E-20;
			var comparer = new MatrixComparer(tol);
			Assert.True(comparer.AreEqual(matrixBExpected, matrixBComputed));
			Assert.Equal(valuesExpected, matrixBComputed.RawValues);
			Assert.Equal(rowIndicesExpected, matrixBComputed.RawRowIndices);
			Assert.Equal(colOffsetsExpected, matrixBComputed.RawColOffsets);
		}
	}
}
