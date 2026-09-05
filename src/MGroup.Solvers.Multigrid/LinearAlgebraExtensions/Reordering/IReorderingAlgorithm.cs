namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering
{
	using MGroup.LinearAlgebra.Reordering;

	/// <summary>
	/// Calculates fill-reducting permutations for the rows/columns of a symmetric sparse matrix.
	/// </summary>
	public interface IReorderingAlgorithm
	{
		/// <summary>
		/// Finds a fill-reducting permutation for the rows/columns of a symmetric sparse matrix, described by its sparsity 
		/// pattern. The returned permutation can be new-to-old or old-to-new.
		/// </summary>
		/// <param name="pattern">The indices of the non-zero entries of a symmetric matrix.</param>
		/// <returns>The permutation array(s) encapsulated inside a <see cref="Permutation"/> object.</returns>
		Permutation FindPermutation(SparsityPatternSymmetric pattern);

		/// <summary>
		/// Finds a fill-reducting permutation for the rows/columns of a symmetric sparse matrix, described by its sparsity
		/// pattern. The returned permutation can be new-to-old or old-to-new.
		/// </summary>
		/// <param name="order">The number of rows (and columns) of the matrix.</param>
		/// <param name="cscRowIndices">
		/// Array that contains the row indices of each non-zero entry of the upper triangle, in symmetric CSC format.
		/// </param>
		/// <param name="cscColOffsets">
		/// Array that contains the offsets into <paramref name="cscRowIndices"/> of each column, in symmetric CSC format.
		/// </param>
		/// <returns>The permutation array(s) encapsulated inside a <see cref="Permutation"/> object.</returns>
		/// permutation: An array containing the fill reducing permutation.
		Permutation FindPermutation(int order, int[] cscRowIndices, int[] cscColOffsets);
	}
}
