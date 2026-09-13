namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;

	public static class DebugUtilities
	{
		public static (int[] rows, int[] columns, double[] values) GetNonZerosColMajor(IReadOnlyMatrix matrix, bool zeroBased = false)
			=> GetNonZerosColMajor((CsrMatrix)matrix, zeroBased);

		public static (int[] rows, int[] columns, double[] values) GetNonZerosColMajor(CsrMatrix csr, bool zeroBased = false)
		{
			var dokColMajor = new SortedDictionary<int, double>[csr.NumColumns];
			for (int j = 0; j < csr.NumColumns; j++)
			{
				dokColMajor[j] = new SortedDictionary<int, double>();
			}

			int nnz = 0;
			foreach (var entry in csr.EnumerateNonZeros())
			{
				dokColMajor[entry.col][entry.row] = entry.value;
				nnz++;
			}

			var rows = new int[nnz];
			var columns = new int[nnz];
			var values = new double[nnz];
			int idxOffset = zeroBased ? 0 : 1;

			int t = 0;
			for (int j = 0; j < csr.NumColumns; j++)
			{
				foreach (var entry in dokColMajor[j])
				{
					int i = entry.Key;
					double val = entry.Value;

					rows[t] = i + idxOffset;
					columns[t] = j + idxOffset;
					values[t] = val;

					t++;
				}
			}

			return (rows, columns, values);
		}

		public static bool HasDuplicateEntries(IReadOnlyMatrix matrix) => HasDuplicateEntries((CsrMatrix)matrix);

		public static bool HasDuplicateEntries(CsrMatrix csr)
		{
			for (int i = 0; i < csr.NumRows; i++)
			{
				int rowStart = csr.RawRowOffsets[i];
				int rowEnd = csr.RawRowOffsets[i + 1];

				var columnMultiplicities = new Dictionary<int, int>();

				for (int t = rowStart; t < rowEnd; t++)
				{
					int j = csr.RawColIndices[t];
					if (columnMultiplicities.ContainsKey(j))
					{
						columnMultiplicities[j]++;
					}
					else
					{
						columnMultiplicities.Add(j, 1);
					}
				}

				foreach (int multiplicity in columnMultiplicities.Values)
				{
					if (multiplicity != 1)
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool HasSortedRows(IReadOnlyMatrix matrix) => HasSortedRows((CsrMatrix)matrix);

		public static bool HasSortedRows(CsrMatrix csr)
		{
			for (int i = 0; i < csr.NumRows; i++)
			{
				int rowStart = csr.RawRowOffsets[i];
				int rowEnd = csr.RawRowOffsets[i + 1];

				int previousColumn = csr.RawColIndices[rowStart];
				for (int t = rowStart + 1; t < rowEnd; t++)
				{
					int j = csr.RawColIndices[t];
					if (j <= previousColumn)
					{
						return false;
					}
					else
					{
						previousColumn = j;
					}
				}
			}

			return true;
		}

		public static double NormFrobernius(IReadOnlyMatrix matrix) => NormFrobernius((ISparseMatrix)matrix);

		public static double NormFrobernius(ISparseMatrix matrix)
		{
			double sum = 0.0;
			foreach (var entry in matrix.EnumerateNonZeros())
			{
				sum += entry.value * entry.value;
			}
			return Math.Sqrt(sum);
		}
	}
}
