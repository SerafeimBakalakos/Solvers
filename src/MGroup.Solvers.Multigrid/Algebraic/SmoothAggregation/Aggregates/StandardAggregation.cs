namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Calculates aggregates using 3 passes, without taking into account the graph edge weights (values of the strength of connection matrix).
	/// Pass 1: A new aggregate is assigned to each connected graph node, if both it and its neighbor nodes are unassigned.
	/// Pass 2: Each connected node that was not assigned previously, is now assigned to the first assigned neighbor met.
	/// Pass 3: Each connected node that was not assigned previously and its unassigned neighbors are assigned to a new aggregate (different per node neighborhood).
	/// Unconnected graph nodes are not assigned to any aggregate.
	/// </summary>
	public class StandardAggregation : IAggregationStrategy
	{
		public AggregateCollection FindAggregates(CsrMatrix soc)
		{
			if (soc.NumRows != soc.NumColumns)
			{
				throw new ArgumentException("Aggregation requires the strength of connection matrix to be square", nameof(soc));
			}

			int n = soc.NumRows;
			var aggregatesOfGraphNodes = new int[n];
			var rootGraphNodesOfAggregates = new int[n];

			// Zero means that a node has not yet been processed.
			// During the algorithm, positive values are temporary 1-based aggregate IDs.
			// Negative values are used to mark nodes that are attached to an existing aggregate or isolated nodes.
			Array.Fill(aggregatesOfGraphNodes, 0);

			int nextAggregate = 1;

			// Pass #1
			// Look for an unaggregated node whose neighbors are all unaggregated.
			// Such a node becomes the root of a new aggregate, and all of its neighbors are placed into that aggregate.
			for (int i = 0; i < n; ++i)
			{
				if (aggregatesOfGraphNodes[i] != 0)
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
						if (aggregatesOfGraphNodes[j] != 0)
						{
							hasAggregatedNeighbors = true;
							break;
						}
					}
				}

				if (!hasNeighbors)
				{
					// Isolated node: do not aggregate it in this pass.
					aggregatesOfGraphNodes[i] = -n;
				}
				else if (!hasAggregatedNeighbors)
				{
					// Make an aggregate from this node and all of its neighbors.
					aggregatesOfGraphNodes[i] = nextAggregate;
					rootGraphNodesOfAggregates[nextAggregate - 1] = i;
					for (int t = rowStart; t < rowEnd; ++t)
					{
						aggregatesOfGraphNodes[soc.RawColIndices[t]] = nextAggregate;
					}

					++nextAggregate;
				}
			}

			// Pass #2
			// Attach every remaining unaggregated node to any neighboring aggregate.
			for (int i = 0; i < n; ++i)
			{
				if (aggregatesOfGraphNodes[i] != 0)
				{
					continue;
				}

				for (int t = soc.RawRowOffsets[i]; t < soc.RawRowOffsets[i + 1]; ++t)
				{
					int j = soc.RawColIndices[t];
					int aggregate = aggregatesOfGraphNodes[j];

					if (aggregate > 0)
					{
						// Use a negative temporary value so that this node is distinguishable from a node that originally belonged to the aggregate.
						aggregatesOfGraphNodes[i] = -aggregate;
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
				int aggregate = aggregatesOfGraphNodes[i];

				if (aggregate != 0)
				{
					if (aggregate > 0)
					{
						aggregatesOfGraphNodes[i] = aggregate - 1;
					}
					else if (aggregate == -n)
					{
						aggregatesOfGraphNodes[i] = -1;
					}
					else
					{
						aggregatesOfGraphNodes[i] = -aggregate - 1;
					}

					continue;
				}

				int rowStart = soc.RawRowOffsets[i];
				int rowEnd = soc.RawRowOffsets[i + 1];

				aggregatesOfGraphNodes[i] = nextAggregate;
				rootGraphNodesOfAggregates[nextAggregate] = i;

				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = soc.RawColIndices[t];
					if (aggregatesOfGraphNodes[j] == 0)
					{
						aggregatesOfGraphNodes[j] = nextAggregate;
					}
				}

				++nextAggregate;
			}

			// Keep only the relevant entries.
			int numAggregates = nextAggregate;
			var rootGraphNodesOfAggregatesFinal = new int[numAggregates];
			Array.Copy(rootGraphNodesOfAggregates, rootGraphNodesOfAggregatesFinal, numAggregates);

			return new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregatesFinal);
		}
	}
}
