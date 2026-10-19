namespace MGroup.Solvers.LinearAlgebraExtensions.Matrices
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.ConstrainedExecution;
	using System.Text;

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

		public static double[] GetDiagonalAbs(this CsrMatrix A)
		{
			int n = A.NumRows;
			var diagonal = new double[n];
			for (int i = 0; i < n; ++i)
			{
				int rowStart = A.RawRowOffsets[i];
				int rowEnd = A.RawRowOffsets[i + 1];
				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = A.RawColIndices[t];
					if (j == i)
					{
						diagonal[i] = Math.Abs(A.RawValues[t]);
						break;
					}
				}
			}

			return diagonal;
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
	}
}
