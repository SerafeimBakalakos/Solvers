namespace MGroup.Solvers.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public static class FullMatrixRowMajorExtensions
	{
		public static FullMatrixRowMajor CreateFromMatrix(IReadOnlyMatrix original)
		{
			int numRows = original.NumRows;
			int numCols = original.NumColumns;
			double[] rowMajor = new double[numRows * numCols];
			int idxCounter = -1;
			for (int i = 0; i < numRows; ++i) // The order of loops is important
			{
				for (int j = 0; j < numCols; ++j)
				{
					rowMajor[++idxCounter] = original[i, j];
				}
			}

			return FullMatrixRowMajor.CreateFromArray(numRows, numCols, rowMajor, copyArray: false);
		}
	}
}
