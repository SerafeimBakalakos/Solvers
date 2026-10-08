namespace MGroup.Solvers.LinearAlgebraExtensions.Commons
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Extensions;
	using MGroup.LinearAlgebra.Matrices;

	public static class CsrCsrBinaryOperations
	{
		/// <summary>
		/// Calculates C = A - B. A and B are CSR matrices with unrelated sparsity patterns. 
		/// Entries that become zero due to cancellation will be stored in C.
		/// </summary>
		/// <param name="matrixA">Matrix A in CSR format, where entries are unique and sorted.</param>
		/// <param name="matrixB">Matrix B in CSR format, where entries are unique and sorted.</param>
		/// <returns>Matrix C in CSR format, where entries are unique and sorted.</returns>
		public static CsrMatrix CsrMinusCsr(CsrMatrix matrixA, CsrMatrix matrixB)
		{
			Preconditions.CheckSameMatrixDimensions(matrixA, matrixB);

			int numRows = matrixA.NumRows;
			int numColumns = matrixA.NumColumns;
			int maxNumNonZeros = matrixA.NumNonZeros + matrixB.NumNonZeros;

			var values = new double[maxNumNonZeros];
			var columnIndices = new int[maxNumNonZeros];
			var rowOffsets = new int[numRows + 1];

			rowOffsets[0] = 0;
			int numNonZeros = 0;

			for (int i = 0; i < numRows; ++i)
			{
				int aPosition = matrixA.RawRowOffsets[i];
				int bPosition = matrixB.RawRowOffsets[i];
				int aEnd = matrixA.RawRowOffsets[i + 1];
				int bEnd = matrixB.RawRowOffsets[i + 1];

				// While not finished with either row.
				while (aPosition < aEnd && bPosition < bEnd)
				{
					int aColumn = matrixA.RawColIndices[aPosition];
					int bColumn = matrixB.RawColIndices[bPosition];

					if (aColumn == bColumn)
					{
						double valueC = matrixA.RawValues[aPosition] - matrixB.RawValues[bPosition];
						columnIndices[numNonZeros] = aColumn;
						values[numNonZeros] = valueC;
						++numNonZeros;

						++aPosition;
						++bPosition;
					}
					else if (aColumn < bColumn)
					{
						columnIndices[numNonZeros] = aColumn;
						values[numNonZeros] = matrixA.RawValues[aPosition];
						++numNonZeros;

						++aPosition;
					}
					else
					{
						columnIndices[numNonZeros] = bColumn;
						values[numNonZeros] = -matrixB.RawValues[bPosition];
						++numNonZeros;

						++bPosition;
					}
				}

				// Tail of A
				while (aPosition < aEnd)
				{
					columnIndices[numNonZeros] = matrixA.RawColIndices[aPosition];
					values[numNonZeros] = matrixA.RawValues[aPosition];
					++numNonZeros;

					++aPosition;
				}

				// Tail of B
				while (bPosition < bEnd)
				{
					columnIndices[numNonZeros] = matrixB.RawColIndices[bPosition];
					values[numNonZeros] = -matrixB.RawValues[bPosition];
					++numNonZeros;

					++bPosition;
				}

				rowOffsets[i + 1] = numNonZeros;
			}

			Array.Resize(ref values, numNonZeros);
			Array.Resize(ref columnIndices, numNonZeros);

			return CsrMatrix.CreateFromArrays(numRows, numColumns, values, columnIndices, rowOffsets, checkInput: false);
		}

		/// <summary>
		/// Calculates C = A - B. A and B are CSR matrices with unrelated sparsity patterns.
		/// Used when A, B, C are in canonical CSR format: no duplicate entries, entries of the same row are sorted based on their column index, no zero entries.
		/// Entries that become zero due to elimination will not be stored.
		/// </summary>
		/// <param name="matrixA">Matrix A in canonical CSR format.</param>
		/// <param name="matrixB">Matrix B in canonical CSR format.</param>
		/// <returns>Matrix C in canonical CSR format.</returns>
		public static CsrMatrix CsrMinusCsrCanonical(CsrMatrix matrixA, CsrMatrix matrixB)
		{
			Preconditions.CheckSameMatrixDimensions(matrixA, matrixB);

			int numRows = matrixA.NumRows;
			int numColumns = matrixA.NumColumns;
			int maxNumNonZeros = matrixA.NumNonZeros + matrixB.NumNonZeros;

			var values = new double[maxNumNonZeros];
			var columnIndices = new int[maxNumNonZeros];
			var rowOffsets = new int[numRows + 1];

			rowOffsets[0] = 0;
			int numNonZeros = 0;

			for (int i = 0; i < numRows; ++i)
			{
				int aPosition = matrixA.RawRowOffsets[i];
				int bPosition = matrixB.RawRowOffsets[i];
				int aEnd = matrixA.RawRowOffsets[i + 1];
				int bEnd = matrixB.RawRowOffsets[i + 1];

				// While not finished with either row.
				while (aPosition < aEnd && bPosition < bEnd)
				{
					int aColumn = matrixA.RawColIndices[aPosition];
					int bColumn = matrixB.RawColIndices[bPosition];

					if (aColumn == bColumn)
					{
						double valueC = matrixA.RawValues[aPosition] - matrixB.RawValues[bPosition];
						if (valueC != 0.0)
						{
							columnIndices[numNonZeros] = aColumn;
							values[numNonZeros] = valueC;
							++numNonZeros;
						}

						++aPosition;
						++bPosition;
					}
					else if (aColumn < bColumn)
					{
						columnIndices[numNonZeros] = aColumn;
						values[numNonZeros] = matrixA.RawValues[aPosition];
						++numNonZeros;

						++aPosition;
					}
					else
					{
						columnIndices[numNonZeros] = bColumn;
						values[numNonZeros] = -matrixB.RawValues[bPosition];
						++numNonZeros;

						++bPosition;
					}
				}

				// Tail of A
				while (aPosition < aEnd)
				{
					columnIndices[numNonZeros] = matrixA.RawColIndices[aPosition];
					values[numNonZeros] = matrixA.RawValues[aPosition];
					++numNonZeros;

					++aPosition;
				}

				// Tail of B
				while (bPosition < bEnd)
				{
					columnIndices[numNonZeros] = matrixB.RawColIndices[bPosition];
					values[numNonZeros] = -matrixB.RawValues[bPosition];
					++numNonZeros;

					++bPosition;
				}

				rowOffsets[i + 1] = numNonZeros;
			}

			Array.Resize(ref values, numNonZeros);
			Array.Resize(ref columnIndices, numNonZeros);

			return CsrMatrix.CreateFromArrays(numRows, numColumns, values, columnIndices, rowOffsets, checkInput: false);
		}

		/// <summary>
		/// Calculates C = A - B. A and B are CSR matrices with unrelated sparsity patterns.
		/// Used when A, B, C are in canonical CSR format: no duplicate entries, entries of the same row are sorted based on their column index, no zero entries.
		/// Entries that become zero due to elimination will not be stored.
		/// </summary>
		/// <param name="matrixA">Matrix A in canonical CSR format.</param>
		/// <param name="matrixB">Matrix B in canonical CSR format.</param>
		/// <returns>Matrix C in canonical CSR format.</returns>
		public static CsrMatrix CsrMinusCsrCanonicalResult(CsrMatrix matrixA, CsrMatrix matrixB)
		{
			Preconditions.CheckSameMatrixDimensions(matrixA, matrixB);

			int numRows = matrixA.NumRows;
			int numColumns = matrixA.NumColumns;
			int maxNumNonZeros = matrixA.NumNonZeros + matrixB.NumNonZeros;

			var values = new double[maxNumNonZeros];
			var columnIndices = new int[maxNumNonZeros];
			var rowOffsets = new int[numRows + 1];

			rowOffsets[0] = 0;
			int numNonZeros = 0;

			for (int i = 0; i < numRows; ++i)
			{
				int aPosition = matrixA.RawRowOffsets[i];
				int bPosition = matrixB.RawRowOffsets[i];
				int aEnd = matrixA.RawRowOffsets[i + 1];
				int bEnd = matrixB.RawRowOffsets[i + 1];

				// While not finished with either row.
				while (aPosition < aEnd && bPosition < bEnd)
				{
					int aColumn = matrixA.RawColIndices[aPosition];
					int bColumn = matrixB.RawColIndices[bPosition];

					if (aColumn == bColumn)
					{
						double valueC = matrixA.RawValues[aPosition] - matrixB.RawValues[bPosition];
						if (valueC != 0.0)
						{
							columnIndices[numNonZeros] = aColumn;
							values[numNonZeros] = valueC;
							++numNonZeros;
						}

						++aPosition;
						++bPosition;
					}
					else if (aColumn < bColumn)
					{
						double valueC = matrixA.RawValues[aPosition];
						if (valueC != 0.0)
						{
							columnIndices[numNonZeros] = aColumn;
							values[numNonZeros] = valueC;
							++numNonZeros;
						}

						++aPosition;
					}
					else
					{
						double valueC = -matrixB.RawValues[bPosition];
						if (valueC != 0.0)
						{
							columnIndices[numNonZeros] = bColumn;
							values[numNonZeros] = valueC;
							++numNonZeros;
						}

						++bPosition;
					}
				}

				// Tail of A
				while (aPosition < aEnd)
				{
					double valueC = matrixA.RawValues[aPosition];
					if (valueC != 0.0)
					{
						columnIndices[numNonZeros] = matrixA.RawColIndices[aPosition];
						values[numNonZeros] = valueC;
						++numNonZeros;
					}

					++aPosition;
				}

				// Tail of B
				while (bPosition < bEnd)
				{
					double valueC = -matrixB.RawValues[bPosition];
					if (valueC != 0.0)
					{
						columnIndices[numNonZeros] = matrixB.RawColIndices[bPosition];
						values[numNonZeros] = valueC;
						++numNonZeros;
					}

					++bPosition;
				}

				rowOffsets[i + 1] = numNonZeros;
			}

			Array.Resize(ref values, numNonZeros);
			Array.Resize(ref columnIndices, numNonZeros);

			return CsrMatrix.CreateFromArrays(numRows, numColumns, values, columnIndices, rowOffsets, checkInput: false);
		}
	}
}
