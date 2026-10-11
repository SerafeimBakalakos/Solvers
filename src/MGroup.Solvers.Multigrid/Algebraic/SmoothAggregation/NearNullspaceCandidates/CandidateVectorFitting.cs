namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Orthogonalization;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;

	public class CandidateVectorFitting
	{
		private readonly double orthonormalizationTolerance;
		private readonly ModifiedGramSchmidt mgs;

		public CandidateVectorFitting(double orthonormalizationTolerance = 1E-10)
		{
			if (orthonormalizationTolerance < 0.0)
			{
				throw new ArgumentOutOfRangeException(nameof(orthonormalizationTolerance));
			}

			this.orthonormalizationTolerance = orthonormalizationTolerance;
			this.mgs = new ModifiedGramSchmidt();
		}

		public (CsrMatrix tentativeProlongation, Matrix coarseCandidates) FitCandidates(AggregateCollection aggregates, Matrix fineCandidates)
		{
			int numFineDofs = aggregates.AggregatesOfFineDofs.Length;
			int numAggregates = aggregates.NumAggregates;
			int numCandidates = fineCandidates.NumColumns;
			int numCoarseDofs = numAggregates * numCandidates;

			if (fineCandidates.NumRows != numFineDofs)
			{
				throw new ArgumentException("The number of candidate rows must equal the number of fine DOFs.", nameof(fineCandidates));
			}

			if (numCandidates == 0)
			{
				throw new ArgumentException("At least one candidate is required.", nameof(fineCandidates));
			}

			if (orthonormalizationTolerance < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(orthonormalizationTolerance));
			}

			int[][] fineDofsByAggregate = aggregates.GatherFineDofsPerAggregate();
			var prolongationValues = new double[numFineDofs * numCandidates];
			var prolongationColumnIndices = new int[numFineDofs * numCandidates];
			var prolongationRowOffsets = new int[numFineDofs + 1];
			var coarseCandidates = Matrix.CreateZero(numCoarseDofs, numCandidates);

			// Fit and orthonormalize the candidates independently on each aggregate.
			for (int aggregate = 0; aggregate < numAggregates; ++aggregate)
			{
				int[] fineDofs = fineDofsByAggregate[aggregate];

				// Extract the fine-level candidate rows belonging to this aggregate.
				Matrix localCandidates = fineCandidates.GetSubmatrixRows(fineDofs);

				// Orthonormalize the local candidate columns and obtain their coarse coefficients.
				(Matrix localBasis, Matrix localCoefficients) = mgs.Calculate(localCandidates, orthonormalizationTolerance);

				// Copy the orthonormalized local candidates to the tentative prolongation
				for (int localRow = 0; localRow < fineDofs.Length; ++localRow)
				{
					int fineDof = fineDofs[localRow];

					for (int candidate = 0; candidate < numCandidates; ++candidate)
					{
						int index = fineDof * numCandidates + candidate;
						prolongationValues[index] = localBasis[localRow, candidate];
						prolongationColumnIndices[index] = aggregate * numCandidates + candidate;
					}
				}

				// Copy the orthonormalization coefficents to the coarse-level candidates
				coarseCandidates.SetSubmatrix(aggregate * numCandidates, 0, localCoefficients);
			}

			// Construct the scalar CSR tentative prolongator.
			for (int fineDof = 0; fineDof < numFineDofs; ++fineDof)
			{
				prolongationRowOffsets[fineDof + 1] = (fineDof + 1) * numCandidates;
			}

			var tentativeProlongation = CsrMatrix.CreateFromArrays(numFineDofs, numCoarseDofs, prolongationValues, prolongationColumnIndices, prolongationRowOffsets, checkInput: false);

			return (tentativeProlongation, coarseCandidates);
		}

		//private static void SetSubmatrix(double[,] matrix, double[,] submatrix, int rowOffset, int columnOffset)
		//{
		//	if (rowOffset < 0 || columnOffset < 0 ||
		//		rowOffset + submatrix.GetLength(0) > matrix.GetLength(0) ||
		//		columnOffset + submatrix.GetLength(1) > matrix.GetLength(1))
		//	{
		//		throw new ArgumentException("The source matrix does not fit within the destination matrix at the specified position.");
		//	}

		//	for (int row = 0; row < submatrix.GetLength(0); ++row)
		//	{
		//		for (int column = 0; column < submatrix.GetLength(1); ++column)
		//		{
		//			matrix[rowOffset + row, columnOffset + column] = submatrix[row, column];
		//		}
		//	}
		//}

		//private static double[,] ExtractRows(double[,] matrix, int[] rowIndices)
		//{
		//	int numRowsSubmatrix = rowIndices.Length;
		//	int numColumns = matrix.GetLength(1);
		//	var submatrix = new double[numRowsSubmatrix, numColumns];

		//	// Copy the candidate values for the fine DOFs in this aggregate.
		//	for (int subRow = 0; subRow < numRowsSubmatrix; ++subRow)
		//	{
		//		int originalRow = rowIndices[subRow];
		//		for (int col = 0; col < numColumns; ++col)
		//		{
		//			submatrix[subRow, col] = matrix[originalRow, col];
		//		}
		//	}

		//	return submatrix;
		//}

		//private static (double[,] basis, double[,] coefficients) OrthonormalizeCandidates(double[,] candidates, double tolerance)
		//{
		//	int numRows = candidates.GetLength(0);
		//	int numCandidates = candidates.GetLength(1);
		//	var basis = (double[,])candidates.Clone();
		//	var coefficients = new double[numCandidates, numCandidates];

		//	// Compute each original column norm to define its rank-deficiency threshold.
		//	var thresholds = new double[numCandidates];
		//	for (int candidate = 0; candidate < numCandidates; ++candidate)
		//	{
		//		thresholds[candidate] = tolerance * VectorNorm(basis, candidate);
		//	}

		//	// Apply modified Gram-Schmidt in candidate-column order.
		//	for (int candidate = 0; candidate < numCandidates; ++candidate)
		//	{
		//		for (int previous = 0; previous < candidate; ++previous)
		//		{
		//			double dotProduct = ColumnDotProduct(basis, candidate, previous);
		//			coefficients[previous, candidate] = dotProduct;
		//			for (int row = 0; row < numRows; ++row)
		//			{
		//				basis[row, candidate] -= dotProduct * basis[row, previous];
		//			}
		//		}

		//		// Normalize the orthogonalized column, or zero it if it is too small.
		//		double norm = VectorNorm(basis, candidate);
		//		if (norm > thresholds[candidate])
		//		{
		//			coefficients[candidate, candidate] = norm;
		//			for (int row = 0; row < numRows; ++row)
		//			{
		//				basis[row, candidate] /= norm;
		//			}
		//		}
		//		else
		//		{
		//			coefficients[candidate, candidate] = 0.0;
		//			for (int row = 0; row < numRows; ++row)
		//			{
		//				basis[row, candidate] = 0.0;
		//			}
		//		}
		//	}

		//	return (basis, coefficients);
		//}

		//private static double ColumnDotProduct(double[,] matrix, int firstColumn, int secondColumn)
		//{
		//	double result = 0.0;

		//	for (int row = 0; row < matrix.GetLength(0); ++row)
		//	{
		//		result += matrix[row, firstColumn] * matrix[row, secondColumn];
		//	}

		//	return result;
		//}

		//private static double VectorNorm(double[,] matrix, int column)
		//{
		//	double sumOfSquares = 0.0;

		//	for (int row = 0; row < matrix.GetLength(0); ++row)
		//	{
		//		double value = matrix[row, column];
		//		sumOfSquares += value * value;
		//	}

		//	return Math.Sqrt(sumOfSquares);
		//}

		public (CsrMatrix tentativeProlongation, double[,] coarseCandidates) ComputeOLD(AggregateCollection aggregates, double[,] candidates)
		{
			int numFineDofs = aggregates.AggregatesOfFineDofs.Length;
			int numAggregates = aggregates.NumAggregates;
			int numCandidates = candidates.GetLength(1);

			if (candidates.GetLength(0) != numFineDofs)
			{
				throw new ArgumentException("The number of candidate rows must match the number of fine DOFs.", nameof(candidates));
			}

			if (numCandidates == 0)
			{
				throw new ArgumentException("At least one candidate vector is required.", nameof(candidates));
			}

			// Determine aggregates
			int[] aggregateSizes = new int[numAggregates];
			var fineDofsOfAggregates = new List<int>[numAggregates];

			for (int j = 0; j < numAggregates; ++j)
			{
				fineDofsOfAggregates[j] = new List<int>();
			}

			for (int i = 0; i < numFineDofs; ++i)
			{
				int aggregate = aggregates.AggregatesOfFineDofs[i];
				aggregateSizes[aggregate]++;
				fineDofsOfAggregates[aggregate].Add(i);
			}

			var prolongatorRows = new List<(int Column, double Value)>[numFineDofs];

			for (int i = 0; i < numFineDofs; ++i)
			{
				prolongatorRows[i] = new List<(int Column, double Value)>();
			}

			var coarseCandidates = new double[numAggregates * numCandidates, numCandidates];

			// Fit the candidate vectors independently on each aggregate.
			for (int j = 0; j < numAggregates; ++j)
			{
				List<int> fineDofs = fineDofsOfAggregates[j];
				int numLocalDofs = fineDofs.Count;

				if (numLocalDofs == 0)
				{
					continue;
				}

				// Copy the fine-level candidates restricted to this aggregate.
				var localCandidates = new double[numLocalDofs, numCandidates];

				for (int i = 0; i < numLocalDofs; ++i)
				{
					for (int bj = 0; bj < numCandidates; ++bj)
					{
						localCandidates[i, bj] = candidates[fineDofs[i], bj];
					}
				}

				// Orthonormalize the local candidate columns.
				for (int bj = 0; bj < numCandidates; ++bj)
				{
					double normSquared = 0.0;

					for (int i = 0; i < numLocalDofs; ++i)
					{
						double value = localCandidates[i, bj];
						normSquared += value * value;
					}

					double norm = Math.Sqrt(normSquared);
					double threshold = orthonormalizationTolerance * norm;

					// Orthogonalize against the preceding candidate columns.
					for (int bi = 0; bi < bj; ++bi)
					{
						double dotProduct = 0.0;

						for (int i = 0; i < numLocalDofs; ++i)
						{
							dotProduct += localCandidates[i, bj] * localCandidates[i, bi];
						}

						for (int i = 0; i < numLocalDofs; ++i)
						{
							localCandidates[i, bj] -= dotProduct * localCandidates[i, bi];
						}

						coarseCandidates[j * numCandidates + bi, bj] = dotProduct;
					}

					// Recompute the norm after orthogonalization.
					normSquared = 0.0;

					for (int i = 0; i < numLocalDofs; ++i)
					{
						double value = localCandidates[i, bj];
						normSquared += value * value;
					}

					norm = Math.Sqrt(normSquared);

					double scale;

					if (norm > threshold)
					{
						scale = 1.0 / norm;
						coarseCandidates[j * numCandidates + bj, bj] = norm;
					}
					else
					{
						scale = 0.0;
						coarseCandidates[j * numCandidates + bj, bj] = 0.0;
					}

					for (int i = 0; i < numLocalDofs; ++i)
					{
						localCandidates[i, bj] *= scale;
					}
				}

				// Store the local orthonormal basis in the global prolongator.
				for (int i = 0; i < numLocalDofs; ++i)
				{
					int fineDof = fineDofs[i];

					for (int bj = 0; bj < numCandidates; ++bj)
					{
						double value = localCandidates[i, bj];

						if (value != 0.0)
						{
							prolongatorRows[fineDof].Add((j * numCandidates + bj, value));
						}
					}
				}
			}

			// Assemble the CSR matrix, omitting explicit zeros.
			int maxNumNonZeros = prolongatorRows.Sum(row => row.Count);
			var values = new double[maxNumNonZeros];
			var columnIndices = new int[maxNumNonZeros];
			var rowOffsets = new int[numFineDofs + 1];

			int nnz = 0;

			for (int i = 0; i < numFineDofs; ++i)
			{
				foreach ((int column, double value) in prolongatorRows[i].OrderBy(entry => entry.Column))
				{
					columnIndices[nnz] = column;
					values[nnz] = value;
					nnz++;
				}

				rowOffsets[i + 1] = nnz;
			}

			var tentativeProlongator = CsrMatrix.CreateFromArrays(numFineDofs, numAggregates * numCandidates, values, columnIndices, rowOffsets, checkInput: false);

			return (tentativeProlongator, coarseCandidates);
		}
	}
}
