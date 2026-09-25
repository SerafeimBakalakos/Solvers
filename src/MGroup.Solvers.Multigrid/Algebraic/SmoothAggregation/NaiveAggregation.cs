namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Very simple implementation of <see cref="IAggregationStrategy"/>. The quality of the coarse space may not be very good.
	/// Each unaggregated node becomes the root of a new aggregate, and all of its currently unaggregated neighbors are added to that aggregate.
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
			var aggregatesOfFineDofs = new int[n];
			var fineDofRootsOfAggregates = new int[n]; // Not all these entries will be used

			// -1 means that the node has not yet been aggregated.
			Array.Fill(aggregatesOfFineDofs, -1);

			int currentAggregate = -1;
			for (int i = 0; i < n; ++i)
			{
				// Skip this dof if it is already aggregated
				if (aggregatesOfFineDofs[i] != -1)
				{
					continue;
				}

				// Make a new aggregate with this this fine dof as root
				currentAggregate++;
				aggregatesOfFineDofs[i] = currentAggregate;
				fineDofRootsOfAggregates[currentAggregate] = i;

				// Add all currently unaggregated neighbors to this aggregate.
				int rowStart = soc.RawRowOffsets[i];
				int rowEnd = soc.RawRowOffsets[i + 1];
				for (int t = rowStart; t < rowEnd; ++t)
				{
					int j = soc.RawColIndices[t];
					if (aggregatesOfFineDofs[j] == -1)
					{
						aggregatesOfFineDofs[j] = currentAggregate;
					}
				}
			}

			// Keep only the relevant entries.
			int numAggregates = currentAggregate + 1;
			var fineDofRootsOfAggregatesFinal = new int[numAggregates];
			Array.Copy(fineDofRootsOfAggregates, fineDofRootsOfAggregatesFinal, numAggregates);

			return new AggregateCollection(aggregatesOfFineDofs, fineDofRootsOfAggregatesFinal);
		}
	}
}
