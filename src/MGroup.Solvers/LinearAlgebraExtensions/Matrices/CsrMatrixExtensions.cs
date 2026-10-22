namespace MGroup.Solvers.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.ConstrainedExecution;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;

	public static class CsrMatrixExtensions
	{
		public static double[] CalcAbsRowSums(this CsrMatrix matrix)
		{
			var rowSums = new double[matrix.NumRows];
			int[] rowOffsets = matrix.RawRowOffsets;
			double[] values = matrix.RawValues;

			for (int row = 0; row < matrix.NumRows; ++row)
			{
				double rowSum = 0.0;
				int rowStart = rowOffsets[row];
				int rowEnd = rowOffsets[row + 1];
				for (int t = rowStart; t < rowEnd; ++t)
				{
					rowSum += Math.Abs(values[t]);
				}

				rowSums[row] = rowSum;
			}

			return rowSums;
		}

		public static double[] GetDiagonalAbs(this CsrMatrix matrix)
		{
			int n = matrix.NumRows;
			var diagonal = new double[n];
			for (int i = 0; i < n; ++i)
			{
				int rowStart = matrix.RawRowOffsets[i];
				int rowEnd = matrix.RawRowOffsets[i + 1];
				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = matrix.RawColIndices[t];
					if (j == i)
					{
						diagonal[i] = Math.Abs(matrix.RawValues[t]);
						break;
					}
				}
			}

			return diagonal;
		}

		/// <summary>
		/// A[i,j] = |A[i,j]| / m(i), where m(i) = max(abs(A[i,j])), j=1,...n.
		/// Namely m(i) is the max of the absolute values of row i.
		/// </summary>
		/// <param name="matrix"></param>
		public static void NormalizeMaxAbs(this CsrMatrix matrix)
		{
			int numRows = matrix.NumRows;
			double[] values = matrix.RawValues;
			int[] colIndices = matrix.RawColIndices;
			int[] rowOffsets = matrix.RawRowOffsets;

			// Normalization:
			for (int i = 0; i < numRows; ++i)
			{
				int rowStart = rowOffsets[i];
				int rowEnd = rowOffsets[i + 1];

				double rowMax = 0.0;

				for (int t = rowStart; t < rowEnd; ++t)
				{
					double abs = Math.Abs(values[t]); // Use absolute values, since strength represents distances.
					values[t] = abs; // Each value loses its sign
					if (abs > rowMax)
					{
						rowMax = abs;
					}
				}

				// Scale each row by its max abs.
				if (rowMax > 0.0)
				{
					for (int t = rowStart; t < rowEnd; ++t)
					{
						values[t] /= rowMax;
					}
				}
			}
		}

		/// <summary>
		/// Each block is replaced by a scalar equal to 0.
		/// </summary>
		/// <param name="A">Must be sorted without duplicates</param>
		/// <param name="rowBlockSize"></param>
		/// <param name="colBlockSize"></param>
		public static CsrMatrix ReduceByBlocksEmpty(this CsrMatrix A, int rowBlockSize, int colBlockSize)
		{
			if (A is null) throw new ArgumentNullException();
			if ((A.NumRows <= 0) || (A.NumColumns <= 0)) throw new NonMatchingDimensionsException();
			if (rowBlockSize <= 0) throw new ArgumentOutOfRangeException(nameof(rowBlockSize));
			if (colBlockSize <= 0) throw new ArgumentOutOfRangeException(nameof(colBlockSize));

			int numBlockRows = 1 + (A.NumRows - 1) / rowBlockSize;
			int numBlockCols = 1 + (A.NumColumns - 1) / colBlockSize;

			// Maximum possible number of output entries. Every stored input entry can contribute to at most one block.
			var resultColIndices = new int[A.NumNonZeros];
			var resultRowOffsets = new int[numBlockRows + 1];

			// Construct the output CSR structure.
			var seen = new int[numBlockCols]; // Zero means unseen. Each block row gets a distinct positive stamp.
			int nnz = 0;
			for (int blockRow = 0; blockRow < numBlockRows; blockRow++)
			{
				int blockStart = nnz;
				int firstRow = blockRow * rowBlockSize;
				int endRow = Math.Min(firstRow + rowBlockSize, A.NumRows);

				// Stamps start at 1, so zero means "not seen".
				int stamp = blockRow + 1;
				for (int row = firstRow; row < endRow; row++)
				{
					for (int k = A.RawRowOffsets[row]; k < A.RawRowOffsets[row + 1]; k++)
					{
						int blockCol = A.RawColIndices[k] / colBlockSize;
						if (seen[blockCol] != stamp)
						{
							seen[blockCol] = stamp;
							resultColIndices[nnz++] = blockCol;
						}
					}
				}

				// Sort only this block row's output columns.
				Array.Sort(resultColIndices, blockStart, nnz - blockStart);
				resultRowOffsets[blockRow] = blockStart;
			}

			// Finalize the arrays
			resultRowOffsets[numBlockRows] = nnz;
			Array.Resize(ref resultColIndices, nnz); // Trim the maximum-capacity array to the actual output size.
			var resultValues = new double[nnz];

			return CsrMatrix.CreateFromArrays(numBlockRows, numBlockCols, resultValues, resultColIndices, resultRowOffsets, checkInput: false);
		}

		/// <summary>
		/// Each block is replaced by a scalar equal to the block's Frobernius norm.
		/// </summary>
		/// <param name="A">Must be sorted without duplicates</param>
		/// <param name="rowBlockSize"></param>
		/// <param name="colBlockSize"></param>
		public static CsrMatrix ReduceByBlocksFroberniusNorm(this CsrMatrix A, int rowBlockSize, int colBlockSize)
		{
			CsrMatrix result = ReduceByBlocksEmpty(A, rowBlockSize, colBlockSize);
			int numBlockRows = result.NumRows;
			int[] resultRowOffsets = result.RawRowOffsets;
			int[] resultColIndices = result.RawColIndices;
			double[] resultValues = result.RawValues;

			// Accumulate squared values into output entries.
			for (int blockRow = 0; blockRow < numBlockRows; blockRow++)
			{
				int firstRow = blockRow * rowBlockSize;
				int endRow = Math.Min(firstRow + rowBlockSize, A.NumRows);

				int blockStart = resultRowOffsets[blockRow];
				int blockEnd = resultRowOffsets[blockRow + 1];

				for (int row = firstRow; row < endRow; row++)
				{
					// Each source row has sorted columns, so its block columns are sorted too.
					// Start from the beginning of the output block row for each source row.
					int p = blockStart;

					for (int k = A.RawRowOffsets[row]; k < A.RawRowOffsets[row + 1]; k++)
					{
						int blockCol = A.RawColIndices[k] / colBlockSize;
						while (p < blockEnd && resultColIndices[p] < blockCol)
						{
							p++;
						}

						if (p >= blockEnd || resultColIndices[p] != blockCol)
						{
							throw new InvalidOperationException("Output structure does not match input.");
						}

						double value = A.RawValues[k];
						resultValues[p] += value * value;
					}
				}
			}

			// Finalization: sqrt(sum of squares).
			for (int i = 0; i < resultValues.Length; i++)
			{
				resultValues[i] = Math.Sqrt(resultValues[i]);
			}

			return result;
		}

		public static void ScaleRows(this CsrMatrix csr, double[] diagonal, bool invertDiagonal)
		{
			if (diagonal.Length != csr.NumRows)
			{
				throw new ArgumentException("The diagonal length must match the number of matrix rows.", nameof(diagonal));
			}

			if (invertDiagonal)
			{
				for (int i = 0; i < csr.NumRows; ++i)
				{
					double scale = 1.0 / diagonal[i];
					int rowStart = csr.RawRowOffsets[i];
					int rowEnd = csr.RawRowOffsets[i + 1];

					for (int t = rowStart; t < rowEnd; ++t)
					{
						csr.RawValues[t] *= scale;
					}
				}
			}
			else
			{
				for (int i = 0; i < csr.NumRows; ++i)
				{
					double scale = diagonal[i];
					int rowStart = csr.RawRowOffsets[i];
					int rowEnd = csr.RawRowOffsets[i + 1];

					for (int t = rowStart; t < rowEnd; ++t)
					{
						csr.RawValues[t] *= scale;
					}
				}
			}
		}

		public static void SetAllNonZeros(this CsrMatrix matrix, double value)
		{
			Array.Fill(matrix.RawValues, value);
		}
	}
}
