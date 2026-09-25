namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

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
		/// <param name="A">Square sparse matrix.</param>
		/// <param name="theta">Strength threshold. Usually [0,1]</param>
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
	}
}
