namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Very simple implementation of <see cref="IAggregationStrategy"/>. The quality of the coarse space may not be very good.
	/// Each unaggregated graph node becomes the root of a new aggregate, and all of its currently unaggregated neighbors are added to that aggregate.
	/// </summary>
	public class NaiveAggregation : IAggregationStrategy
	{
		public AggregateCollection FindAggregates(CsrMatrix soc)
		{
			if (soc.NumRows != soc.NumColumns)
			{
				throw new ArgumentException("Aggregation requires a square matrix.", nameof(soc));
			}

			int n = soc.NumRows;
			var aggregatesOfGraphNodes = new int[n];
			var rootGraphNodesOfAggregates = new int[n]; // Not all these entries will be used

			// -1 means that the graph node has not yet been aggregated.
			Array.Fill(aggregatesOfGraphNodes, -1);

			int currentAggregate = -1;
			for (int i = 0; i < n; ++i)
			{
				// Skip this graph node if it is already aggregated
				if (aggregatesOfGraphNodes[i] != -1)
				{
					continue;
				}

				// Make a new aggregate with this graph node as root
				currentAggregate++;
				aggregatesOfGraphNodes[i] = currentAggregate;
				rootGraphNodesOfAggregates[currentAggregate] = i;

				// Add all currently unaggregated neighbors to this aggregate.
				int rowStart = soc.RawRowOffsets[i];
				int rowEnd = soc.RawRowOffsets[i + 1];
				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = soc.RawColIndices[t];
					if (aggregatesOfGraphNodes[j] == -1)
					{
						aggregatesOfGraphNodes[j] = currentAggregate;
					}
				}
			}

			// Keep only the relevant entries.
			int numAggregates = currentAggregate + 1;
			var rootGraphNodesOfAggregatesFinal = new int[numAggregates];
			Array.Copy(rootGraphNodesOfAggregates, rootGraphNodesOfAggregatesFinal, numAggregates);

			return new AggregateCollection(aggregatesOfGraphNodes, rootGraphNodesOfAggregatesFinal);
		}
	}
}
