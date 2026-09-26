namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class SymmetricStrengthOfConnection
	{
		/// <summary>
		/// Computes symmetric strength-of-connection measure for a real scalar sparse matrix.
		/// A connection (i,j) is retained when
		///     |A[i,j]| >= theta * sqrt(|A[i,i]| * |A[j,j]|)
		/// The resulting entries are then replaced by their magnitudes and scaled by the largest magnitude in each row.
		/// </summary>
		/// <remarks>
		/// This is the scalar CSR/CSR-equivalent algorithm used by PyAMG's symmetric_strength_of_connection.
		/// </remarks>
		/// <param name="A">Square sparse matrix of the linear system.</param>
		/// <param name="theta">Strength threshold. Usually in [0,1]</param>
		/// <returns> Strength matrix C with the same dimensions as A.</returns>
		public static DokRowMajor Compute(DokRowMajor A, double theta = 0.0)
		{
			if (A.NumRows != A.NumColumns)
			{
				throw new ArgumentException("Strength of connection requires a square matrix.", nameof(A));
			}

			if (theta < 0.0)
			{
				throw new ArgumentException("Theta must be non-negative.", nameof(theta));
			}

			// Store |Aii| once because it is used for every connection involving node i.
			int numRows = A.NumRows;
			var diagonal = new double[numRows];
			for (int i = 0; i < numRows; ++i)
			{
				diagonal[i] = Math.Abs(A[i, i]);
			}

			var strength = DokRowMajor.CreateEmpty(numRows, numRows);
			for (int i = 0; i < numRows; ++i)
			{
				double rowMaximum = 0.0;

				// We need to inspect the nonzero entries in row i.
				foreach ((int j, double value) in A.EnumerateNonZerosOfRow(i))
				{
					double magnitude = Math.Abs(value);
					double threshold = theta * Math.Sqrt(diagonal[i] * diagonal[j]);

					if (magnitude >= threshold)
					{
						strength.AddToEntry(i, j, magnitude);
						if (magnitude > rowMaximum)
						{
							rowMaximum = magnitude;
						}
					}
				}

				// Scale every row by its largest retained magnitude.
				if (rowMaximum > 0.0)
				{
					foreach ((int j, double value) in strength.EnumerateNonZerosOfRow(i))
					{
						strength[i, j] = value / rowMaximum;
					}
				}
			}

			return strength;
		}

		/// <summary>
		/// Computes symmetric strength-of-connection measure for a real scalar sparse matrix.
		/// A connection (i,j) is retained when
		///     |A[i,j]| >= theta * sqrt(|A[i,i]| * |A[j,j]|)
		/// The resulting entries are then replaced by their magnitudes and scaled by the largest magnitude in each row.
		/// </summary>
		/// <remarks>
		/// This is the scalar CSR/CSR-equivalent algorithm used by PyAMG's symmetric_strength_of_connection.
		/// </remarks>
		/// <param name="A">Square sparse matrix of the linear system.</param>
		/// <param name="theta">Strength threshold. Usually in [0,1].</param>
		/// <returns> Strength matrix C with the same dimensions as A.</returns>
		public static CsrMatrix Compute(CsrMatrix A, double theta = 0.0)
		{
			if (A.NumRows != A.NumColumns)
			{
				throw new ArgumentException("Strength of connection requires a square matrix.", nameof(A));
			}

			if (theta < 0.0)
			{
				throw new ArgumentException("Theta must be non-negative.", nameof(theta));
			}

			int n = A.NumRows;
			double[] diagonal = FindAbsDiagonal(A); // Store |Aii| once because it is used for every connection involving node i.
			(double[] values, int[] columnIndices, int[] rowOffsets) = BuildStrengthMatrix(A, diagonal, theta);
			NormalizeStrengthMatrix(n, values, columnIndices, rowOffsets);
			return CsrMatrix.CreateFromArrays(n, n, values, columnIndices, rowOffsets, checkInput:false);
		}

		private static (double[] values, int[] columnIndices, int[] rowOffsets) BuildStrengthMatrix(CsrMatrix A, double[] diagonal, double theta)
		{
			// Allocate more space than will actually be used for the strength matrix, which contains a subset of A's nonzeros. 
			double[] strengthValues = new double[A.NumNonZeros];
			int[] strengthColumnIndices = new int[A.NumNonZeros];
			int n = A.NumRows;
			int[] strengthRowOffsets = new int[n + 1];

			// Build the strength matrix row-by-row
			int nnz = 0;
			strengthRowOffsets[0] = 0;
			for (int i = 0; i < n; ++i)
			{
				double thetaSquaredDiagonal = theta * theta * diagonal[i];

				int rowStart = A.RawRowOffsets[i];
				int rowEnd = A.RawRowOffsets[i + 1];

				// Inspect the nonzero entries in row i.
				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = A.RawColIndices[t];
					double value = A.RawValues[t];

					if (i == j)
					{
						// Always retain the diagonal.
						strengthColumnIndices[nnz] = j;
						strengthValues[nnz] = value;
						nnz++;
					}
					else if (value * value >= thetaSquaredDiagonal * diagonal[j])
					{
						// |A[i,j]| >= theta * sqrt(|A[i,i]| * |A[j,j]|)
						strengthColumnIndices[nnz] = j;
						strengthValues[nnz] = value;
						nnz++;
					}
				}

				strengthRowOffsets[i + 1] = nnz;
			}

			// Keep only the important parts of the buffers
			Array.Resize(ref strengthValues, nnz);
			Array.Resize(ref strengthColumnIndices, nnz);

			return (strengthValues, strengthColumnIndices, strengthRowOffsets);
		}

		private static double[] FindAbsDiagonal(CsrMatrix A)
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

		private static void NormalizeStrengthMatrix(int numRows, double[] values, int[] columnIndices, int[] rowOffsets)
		{
			// Normalization:
			for (int i = 0; i < numRows; ++i)
			{
				int rowStart = rowOffsets[i];
				int rowEnd = rowOffsets[i + 1];

				double rowMax = 0.0;

				for (int t = rowStart; t < rowEnd; ++t)
				{
					double abs = Math.Abs(values[t]); // Take absolute values, since strength represents distances.
					values[t] = abs;
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
	}
}
