namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Calculates aggregates using PyAMG's standard aggregation algorithm.
	/// </summary>
	public class StandardAggregation : IAggregationStrategy
	{
		public AggregateCollection FindAggregates(CsrMatrix soc)
		{
			if (soc.NumRows != soc.NumColumns)
			{
				throw new ArgumentException("Aggregation requires a square matrix.", nameof(soc));
			}

			int n = soc.NumRows;
			var aggregatesOfFineDofs = new int[n];
			var fineDofRootsOfAggregates = new int[n];

			// Zero means that a node has not yet been processed.
			// During the algorithm, positive values are temporary 1-based aggregate IDs.
			// Negative values are used to mark nodes that are attached to an existing aggregate or isolated nodes.
			Array.Fill(aggregatesOfFineDofs, 0);

			int nextAggregate = 1;

			// Pass #1
			// Look for an unaggregated node whose neighbors are all unaggregated.
			// Such a node becomes the root of a new aggregate, and all of its neighbors are placed into that aggregate.
			for (int i = 0; i < n; ++i)
			{
				if (aggregatesOfFineDofs[i] != 0)
				{
					continue;
				}

				int rowStart = soc.RawRowOffsets[i];
				int rowEnd = soc.RawRowOffsets[i + 1];

				bool hasAggregatedNeighbors = false;
				bool hasNeighbors = false;

				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = soc.RawColIndices[t];
					if (i != j)
					{
						hasNeighbors = true;
						if (aggregatesOfFineDofs[j] != 0)
						{
							hasAggregatedNeighbors = true;
							break;
						}
					}
				}

				if (!hasNeighbors)
				{
					// Isolated node: do not aggregate it in this pass.
					aggregatesOfFineDofs[i] = -n;
				}
				else if (!hasAggregatedNeighbors)
				{
					// Make an aggregate from this node and all of its neighbors.
					aggregatesOfFineDofs[i] = nextAggregate;
					fineDofRootsOfAggregates[nextAggregate - 1] = i;
					for (int t = rowStart; t < rowEnd; ++t)
					{
						aggregatesOfFineDofs[soc.RawColIndices[t]] = nextAggregate;
					}

					++nextAggregate;
				}
			}

			// Pass #2
			// Attach every remaining unaggregated node to any neighboring aggregate.
			for (int i = 0; i < n; ++i)
			{
				if (aggregatesOfFineDofs[i] != 0)
				{
					continue;
				}

				for (int t = soc.RawRowOffsets[i]; t < soc.RawRowOffsets[i + 1]; ++t)
				{
					int j = soc.RawColIndices[t];
					int aggregate = aggregatesOfFineDofs[j];

					if (aggregate > 0)
					{
						// Use a negative temporary value so that this node is distinguishable from a node that originally belonged to the aggregate.
						aggregatesOfFineDofs[i] = -aggregate;
						break;
					}
				}
			}

			--nextAggregate;

			// Pass #3
			// Convert the temporary 1-based aggregate IDs to zero-based IDs. Isolated nodes become -1.
			// Any node still unaggregated becomes the root of a new aggregate, together with all of its currently unaggregated neighbors.
			for (int i = 0; i < n; ++i)
			{
				int aggregate = aggregatesOfFineDofs[i];

				if (aggregate != 0)
				{
					if (aggregate > 0)
					{
						aggregatesOfFineDofs[i] = aggregate - 1;
					}
					else if (aggregate == -n)
					{
						aggregatesOfFineDofs[i] = -1;
					}
					else
					{
						aggregatesOfFineDofs[i] = -aggregate - 1;
					}

					continue;
				}

				int rowStart = soc.RawRowOffsets[i];
				int rowEnd = soc.RawRowOffsets[i + 1];

				aggregatesOfFineDofs[i] = nextAggregate;
				fineDofRootsOfAggregates[nextAggregate] = i;

				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = soc.RawColIndices[t];
					if (aggregatesOfFineDofs[j] == 0)
					{
						aggregatesOfFineDofs[j] = nextAggregate;
					}
				}

				++nextAggregate;
			}

			// Keep only the relevant entries.
			int numAggregates = nextAggregate;
			var fineDofRootsOfAggregatesFinal = new int[numAggregates];
			Array.Copy(fineDofRootsOfAggregates, fineDofRootsOfAggregatesFinal, numAggregates);

			return new AggregateCollection(aggregatesOfFineDofs, fineDofRootsOfAggregatesFinal);
		}
	}
}
