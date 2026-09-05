namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public static class Permutations
	{
		/// <summary>
		/// </summary>
		/// <param name="original"></param>
		/// <param name="inversePermutation">
		/// If y = P * x, then y[j] = x[ip[j]] foreach j.
		/// </param>
		/// <param name="result"></param>
		public static void PermuteVector(double[] original, int[] inversePermutation, double[] result)
		{
			int length = original.Length;
			for (int i = 0; i < length; i++)
			{
				result[i] = original[inversePermutation[i]];
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="order"></param>
		/// <param name="valuesA"></param>
		/// <param name="rowIndicesA"></param>
		/// <param name="colOffsetsA"></param>
		/// <param name="forwardPermutation">
		/// If y = P * x, then y[p[j]] = x[j] foreach j.
		/// </param>
		/// <param name="valuesB"></param>
		/// <param name="rowIndicesB"></param>
		/// <param name="colOffsetsB">Must be cleared before passing it.</param>
		/// <param name="sort">True to sort the entries of each column, ina scending order of their row index.</param>
		public static void PermuteSymmetricCsc(int order, double[] valuesA, int[] rowIndicesA, int[] colOffsetsA,
			int[] forwardPermutation, double[] valuesB, int[] rowIndicesB, int[] colOffsetsB, bool sort)
		{
			// Count non zeros per column after permutation
			for (int jA = 0; jA < order; jA++)
			{
				int jB = forwardPermutation[jA];
				int start = colOffsetsA[jA];
				int end = colOffsetsA[jA + 1];
				for (int tA = start; tA < end; tA++)
				{
					int iA = rowIndicesA[tA];
					int iB = forwardPermutation[iA];
					if (jB >= iB) // It ends up on the upper triangle
					{
						colOffsetsB[jB]++;
					}
					else // It ends up on the lower triangle, so we store its symmetric instead.
					{
						colOffsetsB[iB]++;
					}
				}
			}

			// Find colOffsetsB by accumulating the nnz per column
			for (int col = 0, cumsum = 0; col < order; col++)
			{
				int temp = colOffsetsB[col];
				colOffsetsB[col] = cumsum;
				cumsum += temp;
			}
			colOffsetsB[order] = valuesA.Length;

			// Move the row indices and values to their new positions
			for (int jA = 0; jA < order; jA++)
			{
				int jB = forwardPermutation[jA];
				int start = colOffsetsA[jA];
				int end = colOffsetsA[jA + 1];
				for (int tA = start; tA < end; tA++)
				{
					int iA = rowIndicesA[tA];
					int iB = forwardPermutation[iA];
					if (jB >= iB) // It ends up on the upper triangle
					{
						int tB = colOffsetsB[jB];
						rowIndicesB[tB] = iB;
						valuesB[tB] = valuesA[tA];

						colOffsetsB[jB]++; // This will point tB to the next entry of the column, but it changes colOffsetsB
					}
					else // It ends up on the lower triangle, so we store its symmetric instead.
					{
						int tB = colOffsetsB[iB];
						rowIndicesB[tB] = jB;
						valuesB[tB] = valuesA[tA];

						colOffsetsB[iB]++; // This will point tB to the next entry of the column, but it changes colOffsetsB
					}
				}
			}

			// Each entry of colOffsetsB now points to the start of the next column. Shift them back.
			for (int jB = 0, last = 0; jB <= order; jB++)
			{
				int temp = colOffsetsB[jB];
				colOffsetsB[jB] = last;
				last = temp;
			}

			// Sort row indices of each column
			if (sort)
			{
				for (int jB = 0; jB < order; jB++)
				{
					int start = colOffsetsB[jB];
					int end = colOffsetsB[jB + 1];
					Array.Sort(rowIndicesB, valuesB, start, end - start);
				}
			}
		}
	}
}
