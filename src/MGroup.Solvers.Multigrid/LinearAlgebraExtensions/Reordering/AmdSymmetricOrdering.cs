namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using CSparse.Ordering;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Matrices.Builders;
	using MGroup.LinearAlgebra.Reordering;

	/// <summary>
	/// Calculates a fill-reducing permutation for the rows/columns of a symmetric sparse matrix, using the Approximate Minimum
	/// Degree (AMD) ordering algorithm.
	/// For more information, see the AMD user guide, which is distributed as part of the SuiteSparse library.
	/// </summary>
	public class AmdSymmetricOrdering : IReorderingAlgorithm
	{
		private readonly IImplementationProvider provider;

		public AmdSymmetricOrdering(IImplementationProvider provider = null)
		{
			if (provider == null)
			{
				this.provider = LibrarySettings.GlobalProvider;
			}
			else
			{
				this.provider = provider;
			}
		}

		public Permutation FindPermutation(SparsityPatternSymmetric pattern)
		{
			(int[] rowIndices, int[] colOffsets) = pattern.BuildSymmetricCSCArrays(sortRowsOfEachCol: true);
			return FindPermutation(pattern.Order, rowIndices, colOffsets);
		}

		public Permutation FindPermutation(int order, int[] cscRowIndices, int[] cscColOffsets)
		{
			(int[] permArray, _) = provider.Reordering.AmdSymmetric(order, cscRowIndices, cscColOffsets);
			var permutation = Permutation.CreateWithInversePermutation(permArray); // AMD returns the inverse permutation array
			return permutation;
		}

		/// <summary>
		/// Finds a fill-reducting permutation for the rows/columns of a symmetric sparse matrix in DOK format.
		/// </summary>
		/// <remarks>
		/// The returned permutation is always new-to-old when using AMD, namely reordered[i] = original[permutation[i]].
		/// </remarks>
		/// <param name="dok">The symmetric sparse matrix in DOK format.</param>
		/// <returns>
		/// permutation: The permutation array(s) encapsulated inside a <see cref="Permutation"/> object.
		/// stats: Measuments taken during the execution of the reordering algorithm.
		/// </returns>
		public (Permutation permutation, ReorderingStatistics stats) FindPermutation(DokSymmetric dok)
		{
			(double[] values, int[] rowIndices, int[] colOffsets) = dok.BuildSymmetricCscArrays(true);
			(int[] permArray, ReorderingStatistics stats) =
				provider.Reordering.AmdSymmetric(dok.NumColumns, rowIndices, colOffsets);
			var permutation = Permutation.CreateWithInversePermutation(permArray); // AMD returns the inverse permutation array
			return (permutation, stats);
		}
	}
	}
