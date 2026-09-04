namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Matrices;

	public static class Conversions
	{
		public static CscMatrix CsrToCsc(CsrMatrix csr)
		{
			int nnz = csr.NumNonZeros;
			var cscValues = new double[nnz];
			var cscRowIndices = new int[nnz];
			var cscColOffsets = new int[csr.NumColumns + 1];
			CsrToCsc(csr.NumRows, csr.NumColumns, csr.RawRowOffsets, csr.RawColIndices, csr.RawValues, cscColOffsets, cscRowIndices, cscValues);

			return CscMatrix.CreateFromArrays(csr.NumRows, csr.NumColumns, cscValues, cscRowIndices, cscColOffsets, false);
		}

		public static SymmetricCscMatrix CsrToSymmetricCsc(CsrMatrix csr)
		{
			Preconditions.CheckSquare(csr);

			int numRows = csr.NumRows;
			int numColumns = csr.NumColumns;
			double[] valuesA = csr.RawValues;
			int[] colIndicesA = csr.RawColIndices;
			int[] rowOffsetsA = csr.RawRowOffsets;

			// Count the number of non-zero entries per column of A
			int[] upperOffsetsA = FindUpperTriangleOffsetsCsr(numRows, rowOffsetsA, colIndicesA);
			var colOffsetsB = new int[numColumns + 1];
			for (int row = 0; row < numRows; row++)
			{
				int start = upperOffsetsA[row];
				int end = rowOffsetsA[row + 1];
				for (int t = start; t < end; t++)
				{
					int col = colIndicesA[t];
					colOffsetsB[col]++;
				}
			}

			// Find colOffsetsB by accumulating the nnz of the upper triangle per column
			int nnzUpper = 0;
			for (int col = 0; col < numColumns; col++)
			{
				int temp = colOffsetsB[col];
				colOffsetsB[col] = nnzUpper;
				nnzUpper += temp;
			}
			colOffsetsB[numColumns] = nnzUpper;

			// Find rowIndicesB and valuesB
			var rowIndicesB = new int[nnzUpper];
			var valuesB = new double[nnzUpper];
			for (int row = 0; row < numRows; row++)
			{
				int start = upperOffsetsA[row];
				int end = rowOffsetsA[row + 1];
				for (int t = start; t < end; t++)
				{
					int col = colIndicesA[t];
					int dest = colOffsetsB[col];

					rowIndicesB[dest] = row;
					valuesB[dest] = valuesA[t];

					colOffsetsB[col]++; // This will point dest to the next entry of the column, but it changes colOffsetsB
				}
			}

			// Each entry of colOffsetsB now points to the start of the next column. Shift them back.
			for (int col = 0, last = 0; col <= numColumns; col++)
			{
				int temp = colOffsetsB[col];
				colOffsetsB[col] = last;
				last = temp;
			}

			return SymmetricCscMatrix.CreateFromArrays(numRows, valuesB, rowIndicesB, colOffsetsB, false);
		}

		/// <summary>
		/// Copied from 
		/// https://github.com/scipy/scipy/blob/3b36a574dc657d1ca116f6e230be694f3de31afc/scipy/sparse/sparsetools/csr.h#L376.
		/// Compute B = A for CSR matrix A, CSC matrix B.
		/// Also, with the appropriate arguments can also be used to:
		///   - compute B = A ^ t for CSR matrix A, CSR matrix B
		///   - compute B = A ^ t for CSC matrix A, CSC matrix B
		///   - convert CSC->CSR
		/// Complexity: Linear.  Specifically O(nnz(A) + max(numRows,numColumns))
		/// </summary>
		/// <param name="numRows">Number of rows in A.</param>
		/// <param name="numColumns">Number of columns in A.</param>
		/// <param name="rowOffsetsA">Row pointers. Size = numRows+1.</param>
		/// <param name="colIndicesA">Column indices. Size = nnz(A). They are not assumed to be in sorted order.</param>
		/// <param name="valuesA">Non-zero values. Size = nnz(A).</param>
		/// <param name="colOffsetsB">Preallocated ouput argument. Column pointers. Size = numColumns+1</param>
		/// <param name="rowIndicesB">Preallocated ouput argument. Row indices. Size = nnz(A). They will be in sorted order.</param>
		/// <param name="valuesB">Preallocated ouput argument. Non-zero values. Size = nnz(A).</param>
		internal static void CsrToCsc(int numRows, int numColumns, int[] rowOffsetsA, int[] colIndicesA, double[] valuesA, int[] colOffsetsB, int[] rowIndicesB, double[] valuesB)
		{
			int nnz = rowOffsetsA[numRows];

			// Count the number of non-zero entries per column of A
			for (int n = 0; n < nnz; n++)
			{
				colOffsetsB[colIndicesA[n]]++;
			}

			// Find colOffsetsB by accumulating the nnz per column
			for (int col = 0, cumsum = 0; col < numColumns; col++)
			{
				int temp = colOffsetsB[col];
				colOffsetsB[col] = cumsum;
				cumsum += temp;
			}
			colOffsetsB[numColumns] = nnz;

			// Find rowIndicesB and valuesB
			for (int row = 0; row < numRows; row++)
			{
				int start = rowOffsetsA[row];
				int end = rowOffsetsA[row + 1];
				for (int t = start; t < end; t++)
				{
					int col = colIndicesA[t];
					int dest = colOffsetsB[col];

					rowIndicesB[dest] = row;
					valuesB[dest] = valuesA[t];

					colOffsetsB[col]++; // This will point dest to the next entry of the column, but it changes colOffsetsB
				}
			}

			// Each entry of colOffsetsB now points to the start of the next column. Shift them back.
			for (int col = 0, last = 0; col <= numColumns; col++)
			{
				int temp = colOffsetsB[col];
				colOffsetsB[col] = last;
				last = temp;
			}
		}

		private static int[] FindUpperTriangleOffsetsCsr(int numRows, int[] rowOffsets, int[] colIndices)
		{
			var upperOffsets = new int[numRows];
			for (int row = 0; row < numRows; row++)
			{
				for (int k = rowOffsets[row]; k < rowOffsets[row + 1]; k++)
				{
					int col = colIndices[k];
					if (col >= row)
					{
						upperOffsets[row] = k;
						break;
					}
				}
			}
			return upperOffsets;
		}
	}
}
