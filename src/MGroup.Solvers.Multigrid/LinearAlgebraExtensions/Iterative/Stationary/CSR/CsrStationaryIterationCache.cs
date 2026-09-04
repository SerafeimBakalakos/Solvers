namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class CsrStationaryIterationCache
	{
		private readonly object syncLock = new();

		private readonly Dictionary<CsrPatternKey, int[]> cachedDiagOffsets = new();
		private readonly Dictionary<CsrPatternKey, HashSet<CsrStationaryIterationBase>> allClients = new();

		public int[]? GetOrCreateDiagOffsets(CsrMatrix matrix, Func<int[]> createDiagOffsets)
		{
			var key = new CsrPatternKey(matrix);
			lock (syncLock)
			{
				if (cachedDiagOffsets.TryGetValue(key, out int[] diagOffsets))
				{
					return diagOffsets;
				}
				else
				{
					diagOffsets = createDiagOffsets(); // Potentially slow operation
					cachedDiagOffsets[key] = diagOffsets;
					return diagOffsets;
				}
			}
		}

		public void Register(CsrMatrix matrix, CsrStationaryIterationBase client)
		{
			var key = new CsrPatternKey(matrix);

			lock (syncLock)
			{
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
		}

		public void Unregister(CsrMatrix matrix, CsrStationaryIterationBase client)
		{
			var key = new CsrPatternKey(matrix);
			lock (syncLock)
			{
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
