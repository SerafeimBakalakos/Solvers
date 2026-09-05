namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;

	public static class SymmetricCscMatrixExtensions
	{
		public static SymmetricCscMatrix PermuteRowsAndCols(this SymmetricCscMatrix matrix, Permutation permutation)
		{
			PreconditionsExtensions.CheckRowAndColumnPermutation(matrix, permutation);

			int n = matrix.NumColumns;
			int nnz = matrix.NumNonZerosUpper;
			var valuesB = new double[nnz];
			var rowIndicesB = new int[nnz];
			var colOffsetsB = new int[n + 1];

			int[] forwardPermutation = permutation.PermutationArrayLazy;
			Permutations.PermuteSymmetricCsc(n, matrix.RawValues, matrix.RawRowIndices, matrix.RawColOffsets,
				forwardPermutation, valuesB, rowIndicesB, colOffsetsB, sort: true);

			return SymmetricCscMatrix.CreateFromArrays(n, valuesB, rowIndicesB, colOffsetsB, false);
		}
	}
}
