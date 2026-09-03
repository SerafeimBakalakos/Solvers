namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class CsrStationaryIterationCache
	{
		private readonly Dictionary<CsrPatternKey, int[]> cachedDiagOffsets = new();
		private readonly Dictionary<CsrPatternKey, HashSet<CsrStationaryIterationBase>> allClients = new();

		public void Register(CsrMatrix matrix, CsrStationaryIterationBase client)
		{
			var key = new CsrPatternKey(matrix);
			if (!cachedDiagOffsets.ContainsKey(key))
			{
				throw new ArgumentException("Cannot register a client for a matrix whose diagonal offsets are not cached.");
			}

			if (!allClients.TryGetValue(key, out HashSet<CsrStationaryIterationBase> clients))
			{
				clients = new HashSet<CsrStationaryIterationBase>();
				allClients.Add(key, clients);
			}

			clients.Add(client);
		}

		public void StoreDiagOffsets(CsrMatrix matrix, int[] diagonalOffsets)
		{
			var key = new CsrPatternKey(matrix);
			bool isNew = cachedDiagOffsets.TryAdd(key, diagonalOffsets);
			if (!isNew)
			{
				throw new ArgumentException("The diagonal offsets for this matrix are already stored.");
			}
		}

		public int[]? TryGetDiagOffsetsFor(CsrMatrix matrix)
		{
			var key = new CsrPatternKey(matrix);
			cachedDiagOffsets.TryGetValue(key, out int[] diagOffsets);
			return diagOffsets;
		}

		public void Unregister(CsrMatrix matrix, CsrStationaryIterationBase client)
		{
			var key = new CsrPatternKey(matrix);
			if (allClients.TryGetValue(key, out HashSet<CsrStationaryIterationBase> clients))
			{
				clients.Remove(client);

				if (clients.Count == 0)
				{
					cachedDiagOffsets.Remove(key);
					allClients.Remove(key);
				}
			}
		}

		private struct CsrPatternKey
		{
			public CsrPatternKey(CsrMatrix matrix)
			{
				ColIndices = matrix.RawColIndices;
				RowOffsets = matrix.RawRowOffsets;
			}

			public int[] ColIndices { get; }

			public int[] RowOffsets { get; }
		}
	}
}
