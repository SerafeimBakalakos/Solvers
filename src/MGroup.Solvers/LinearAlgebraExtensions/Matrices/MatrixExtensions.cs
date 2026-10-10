namespace MGroup.Solvers.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public static class MatrixExtensions
	{
		/// <summary>
		/// Extracts a submatrix that contains specific rows of the original one.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="rowIndices">The indices of the rows to keep. Must be in [0, NumRows).</param>
		/// <returns></returns>
		public static Matrix GetSubmatrixRows(this Matrix matrix, int[] rowIndices)
		{
			int numRowsSubmatrix = rowIndices.Length;
			int numRowsOriginal = matrix.NumRows;
			int numColumns = matrix.NumColumns;

			double[] original = matrix.RawData;
			double[] submatrix = new double[numColumns * numRowsSubmatrix];
			
			int idxCounter = -1;
			for (int j = 0; j < numColumns; ++j)
			{
				int colOffset = j * numRowsOriginal;
				foreach (int i in rowIndices)
				{
					submatrix[++idxCounter] = original[colOffset + i];
				}
			}

			return Matrix.CreateFromArray(submatrix, numRowsSubmatrix, numColumns, copyArray: false);
		}
	}
}
