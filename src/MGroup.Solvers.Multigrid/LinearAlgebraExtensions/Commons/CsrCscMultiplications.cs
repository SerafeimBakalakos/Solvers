namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Matrices;

	public static class CsrCscMultiplications
	{
		/// <summary>
		/// Caclulates C = A * B, where A is in CSR format, B in CSC format and C in CSC format.
		/// </summary>
		/// <param name="matrixA">Must be sorted.</param>
		/// <param name="matrixB">Must be sorted.</param>
		/// <returns></returns>
		public static CscMatrix CsrTimesCscToCsc(CsrMatrix matrixA, CscMatrix matrixB)
		{
			Preconditions.CheckMultiplicationDimensions(matrixA, matrixB);

			var numRowsC = matrixA.NumRows;
			var numColsC = matrixB.NumColumns;

			// First calculate every column and store the non zeros temporarily
			var columns = new List<(int row, double value)>[numColsC];
			var numNonZeros = 0;
			for (var j = 0; j < numColsC; j++)
			{
				var column = new List<(int row, double value)>();
				for (var i = 0; i < numRowsC; i++)
				{
					(var dotProduct, var structuralZero) = DotProduct(matrixA, i, matrixB, j);
					if (!structuralZero)
					{
						column.Add((i, dotProduct)); // If the dotProduct is zero due to terms cancelling out, we explicitly store it.
						numNonZeros++;
					}
				}

				columns[j] = column;
			}

			// Convert temporary columns to CSC.
			var colOffsetsC = new int[numColsC + 1];
			var rowIndicesC = new int[numNonZeros];
			var valuesC = new double[numNonZeros];

			var posC = 0;
			for (var j = 0; j < numColsC; j++)
			{
				colOffsetsC[j] = posC;
				foreach ((var row, var value) in columns[j])
				{
					rowIndicesC[posC] = row;
					valuesC[posC] = value;
					posC++;
				}
			}

			colOffsetsC[numColsC] = posC;

			return CscMatrix.CreateFromArrays(numRowsC, numColsC, valuesC, rowIndicesC, colOffsetsC, checkInput: false);
		}

		/// <summary>
		/// Caclulates C = A * B, where A is in CSR format, B in CSC format and C in CSR format.
		/// </summary>
		/// <param name="matrixA">Must be sorted.</param>
		/// <param name="matrixB">Must be sorted.</param>
		/// <returns></returns>
		public static CsrMatrix CsrTimesCscToCsr(CsrMatrix matrixA, CscMatrix matrixB)
		{
			Preconditions.CheckMultiplicationDimensions(matrixA, matrixB);

			var numRowsC = matrixA.NumRows;
			var numColsC = matrixB.NumColumns;

			// First calculate every row and store the non zeros temporarily
			var rows = new List<(int col, double value)>[numRowsC];
			var numNonZeros = 0;
			for (var i = 0; i < numRowsC; i++)
			{
				var row = new List<(int col, double value)>();
				for (var j = 0; j < numColsC; j++)
				{
					(var dotProduct, var structuralZero) = DotProduct(matrixA, i, matrixB, j);
					if (!structuralZero)
					{
						row.Add((j, dotProduct)); // If the dotProduct is zero due to terms cancelling out, we explicitly store it.
						numNonZeros++;
					}
				}

				rows[i] = row;
			}

			// Convert temporary rows to CSR.
			var rowOffsetsC = new int[numRowsC + 1];
			var colIndicesC = new int[numNonZeros];
			var valuesC = new double[numNonZeros];

			var posC = 0;
			for (var i = 0; i < numRowsC; i++)
			{
				rowOffsetsC[i] = posC;
				foreach ((var col, var value) in rows[i])
				{
					colIndicesC[posC] = col;
					valuesC[posC] = value;
					posC++;
				}
			}

			rowOffsetsC[numRowsC] = posC;

			return CsrMatrix.CreateFromArrays(numRowsC, numColsC, valuesC, colIndicesC, rowOffsetsC, checkInput: false);
		}

		private static (double dotProduct, bool structuralZero) DotProduct(CsrMatrix matrixA, int rowA, CscMatrix matrixB, int colB)
		{
			var posA = matrixA.RawRowOffsets[rowA];
			var endA = matrixA.RawRowOffsets[rowA + 1];

			var posB = matrixB.RawColOffsets[colB];
			var endB = matrixB.RawColOffsets[colB + 1];

			var dotProduct = 0.0;
			var structuralZero = true;
			while (posA < endA && posB < endB)
			{
				var colA = matrixA.RawColIndices[posA];
				var rowB = matrixB.RawRowIndices[posB];

				if (colA < rowB)
				{
					posA++;
				}
				else if (colA > rowB)
				{
					posB++;
				}
				else // Found a matching structural entry in A and B.
				{
					structuralZero = false;
					dotProduct += matrixA.RawValues[posA] * matrixB.RawValues[posB];
					posA++;
					posB++;
				}
			}

			return (dotProduct, structuralZero);
		}
	}
}
