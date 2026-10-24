namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	/// <summary>
	/// Calculates the symmetric strength-of-connection measure of real scalar symmetric sparse matrices.
	/// If A is the input matrix and theta a strength thresjold, then a connection (i,j) is retained when
	///		|A[i,j]| >= theta * sqrt(|A[i,i]| * |A[j,j]|)
	/// The resulting entries are then replaced by their magnitudes and scaled by the largest magnitude in each row.
	/// </summary>
	/// <remarks>
	/// This is the scalar CSR/CSR-equivalent algorithm used by PyAMG's symmetric_strength_of_connection.
	/// </remarks>
	public class SymmetricStrengthOfConnection : IStrengthOfConnection
	{
		private readonly double theta;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="theta">Strength threshold. Usually in [0,1].</param>
		public SymmetricStrengthOfConnection(double theta = 0.0)
		{
			this.theta = theta;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="A"></param>
		/// <param name="blockSize">
		/// The block size must be the same for rows and columns, when a symmetric connection strength strategy is used.
		/// </param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public CsrMatrix CalcStrengthMatrix(CsrMatrix A, int blockSize)
		{
			if (A.NumRows != A.NumColumns)
			{
				throw new ArgumentException("Strength of connection requires a square matrix.", nameof(A));
			}

			if (theta < 0.0)
			{
				throw new ArgumentException("Theta must be non-negative.", nameof(theta));
			}

			if (blockSize < 1 || A.NumRows % blockSize != 0)
			{
				throw new ArgumentException("Block size must be positive and divide the matrix dimensions.", nameof(blockSize));
			}

			// Assumes the CSR matrix has unique, sorted column indices in each row.
			if (blockSize == 1)
			{
				// If theta==0, the thresholding operation returns the same matrix.
				CsrMatrix strength = (theta == 0) ? A.CopyAsSameType(copyIndexingData: false) : BuildScalarStrengthMatrix(A, theta);
				strength.NormalizeMaxAbs();
				return strength;
			}
			else
			{
				if (theta == 0.0)
				{

					CsrMatrix reducedMatrix = A.ReduceByBlocksEmpty(blockSize, blockSize);
					reducedMatrix.SetAllNonZeros(1.0);
					return reducedMatrix; // Thresholding and normalization are redundant here, since all entries are 1.
				}
				else
				{
					CsrMatrix reducedMatrix = A.ReduceByBlocksFroberniusNorm(blockSize, blockSize);
					CsrMatrix strength = BuildScalarStrengthMatrix(reducedMatrix, theta);
					strength.NormalizeMaxAbs();
					return strength;
				}
			}
		}

		public DokRowMajor ComputeScalar(DokRowMajor A)
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

		//private static CsrMatrix BuildBlockGraphStrengthMatrix(CsrMatrix A, int blockSize)
		//{
		//	int numBlocks = A.NumRows / blockSize;
		//	var blockColumnsByRow = new SortedSet<int>[numBlocks];

		//	for (int blockRow = 0; blockRow < numBlocks; ++blockRow)
		//	{
		//		blockColumnsByRow[blockRow] = new SortedSet<int>();
		//	}

		//	int[] rowOffsets = A.RawRowOffsets;
		//	int[] columnIndices = A.RawColIndices;

		//	for (int row = 0; row < A.NumRows; ++row)
		//	{
		//		int blockRow = row / blockSize;

		//		for (int k = rowOffsets[row]; k < rowOffsets[row + 1]; ++k)
		//		{
		//			int blockColumn = columnIndices[k] / blockSize;
		//			blockColumnsByRow[blockRow].Add(blockColumn);
		//		}
		//	}

		//	var values = new List<double>();
		//	var columns = new List<int>();
		//	var offsets = new int[numBlocks + 1];

		//	offsets[0] = 0;

		//	for (int blockRow = 0; blockRow < numBlocks; ++blockRow)
		//	{
		//		foreach (int blockColumn in blockColumnsByRow[blockRow])
		//		{
		//			columns.Add(blockColumn);
		//			values.Add(1.0);
		//		}

		//		offsets[blockRow + 1] = values.Count;
		//	}

		//	return CsrMatrix.CreateFromArrays(
		//		numBlocks, numBlocks, values.ToArray(), columns.ToArray(), offsets, checkInput: false);
		//}

		//private static CsrMatrix BuildBlockFrobeniusNormMatrix(CsrMatrix A, int blockSize)
		//{
		//	int numBlocks = A.NumRows / blockSize;
		//	var blockNormsSquaredByRow = new SortedDictionary<int, double>[numBlocks];
		//	for (int blockRow = 0; blockRow < numBlocks; ++blockRow)
		//	{
		//		blockNormsSquaredByRow[blockRow] = new SortedDictionary<int, double>();
		//	}

		//	int[] rowOffsets = A.RawRowOffsets;
		//	int[] columnIndices = A.RawColIndices;
		//	double[] values = A.RawValues;

		//	for (int row = 0; row < A.NumRows; ++row)
		//	{
		//		int blockRow = row / blockSize;

		//		for (int k = rowOffsets[row]; k < rowOffsets[row + 1]; ++k)
		//		{
		//			int blockColumn = columnIndices[k] / blockSize;
		//			double value = values[k];

		//			if (blockNormsSquaredByRow[blockRow].TryGetValue(blockColumn, out double sumSquares))
		//			{
		//				blockNormsSquaredByRow[blockRow][blockColumn] = sumSquares + value * value;
		//			}
		//			else
		//			{
		//				blockNormsSquaredByRow[blockRow].Add(blockColumn, value * value);
		//			}
		//		}
		//	}

		//	var normValues = new List<double>();
		//	var normColumns = new List<int>();
		//	var normOffsets = new int[numBlocks + 1];

		//	normOffsets[0] = 0;

		//	for (int blockRow = 0; blockRow < numBlocks; ++blockRow)
		//	{
		//		foreach ((int blockColumn, double sumSquares) in blockNormsSquaredByRow[blockRow])
		//		{
		//			normColumns.Add(blockColumn);
		//			normValues.Add(Math.Sqrt(sumSquares));
		//		}

		//		normOffsets[blockRow + 1] = normValues.Count;
		//	}

		//	return CsrMatrix.CreateFromArrays(numBlocks, numBlocks, normValues.ToArray(), normColumns.ToArray(), normOffsets, checkInput: false);
		//}

		private static CsrMatrix BuildScalarStrengthMatrix(CsrMatrix A, double theta)
		{
			// Store |Aii| once because it is used for every connection involving node i.
			double[] diagonal = A.GetDiagonalAbs(); 

			// Allocate more space than will actually be used for the strength matrix, which contains a subset of A's nonzeros. 
			double[] strengthValues = new double[A.NumNonZeros];
			int[] strengthColumnIndices = new int[A.NumNonZeros];
			int n = A.NumRows;
			int[] strengthRowOffsets = new int[n + 1];

			// Build the strength matrix row-by-row
			int nnz = 0;
			strengthRowOffsets[0] = 0;
			double thetaSquared = theta * theta;
			for (int i = 0; i < n; ++i)
			{
				double thetaSquaredDiagonal = thetaSquared * diagonal[i];

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

			return CsrMatrix.CreateFromArrays(A.NumRows, A.NumColumns, strengthValues, strengthColumnIndices, strengthRowOffsets, checkInput: false);
		}
	}
}
