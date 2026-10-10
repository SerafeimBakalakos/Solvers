namespace MGroup.Solvers.LinearAlgebraExtensions.Orthogonalization
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using CSparse;

	using MGroup.LinearAlgebra.Matrices;

	using static MGroup.LinearAlgebra.LibrarySettings;

	public class ModifiedGramSchmidt
	{
		/// <summary>
		/// Implements A = Q * R factorization using the Gram-Schmidt algorithm. Very small basis vectors are zeroed out. 
		/// </summary>
		/// <param name="matrixA">The original matrix</param>
		/// <param name="tolerance">Basis vectors whose norm is less than this tolerance are zeroed out and can be ignored.</param>
		/// <returns>
		/// matrixQ: Full matrix with the orthonormalized columns of <paramref name="matrixA"/>.
		/// matrixR: Upper triangular matrix containing the orthonormalization coefficients.
		/// </returns>
		public (Matrix matrixQ, Matrix matrixR) Calculate(Matrix matrixA, double tolerance)
		{
			int numRowsQ = matrixA.NumRows;
			int numColumns = matrixA.NumColumns;
			double[] matrixQ = matrixA.CopyToFullMatrix().RawData;
			var matrixR = Matrix.CreateZero(numColumns, numColumns);

			// Compute each original column norm to define its rank-deficiency threshold.
			var thresholds = new double[numColumns];
			for (int col = 0; col < numColumns; ++col)
			{
				double norm = GlobalProvider.Blas.Dnrm2(numRowsQ, matrixQ, col * numRowsQ, 1);
				thresholds[col] = tolerance * norm;
			}

			// Apply modified Gram-Schmidt in column order.
			for (int col = 0; col < numColumns; ++col)
			{
				for (int previous = 0; previous < col; ++previous)
				{
					double dotProduct = GlobalProvider.Blas.Ddot(numRowsQ, matrixQ, col * numRowsQ, 1, matrixQ, previous * numRowsQ, 1);
					matrixR[previous, col] = dotProduct;

					GlobalProvider.Blas.Daxpy(numRowsQ, -dotProduct, matrixQ, previous * numRowsQ, 1, matrixQ, col * numRowsQ, 1);
				}

				// Normalize the orthogonalized column, or zero it if it is too small.
				double norm = GlobalProvider.Blas.Dnrm2(numRowsQ, matrixQ, col * numRowsQ, 1);
				if (norm > thresholds[col])
				{
					matrixR[col, col] = norm;
					GlobalProvider.Blas.Dscal(numRowsQ, 1.0 / norm, matrixQ, col * numRowsQ, 1);
				}
				else
				{
					matrixR[col, col] = 0.0;
					Array.Clear(matrixQ, col * numRowsQ, numRowsQ);
				}
			}

			return (Matrix.CreateFromArray(matrixQ, numRowsQ, numColumns, copyArray: false), matrixR);
		}
	}
}
