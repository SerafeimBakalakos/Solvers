namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public sealed class AggregateCollection
	{
		public AggregateCollection(int[] aggregatesOfGraphNodes, int[] rootGraphNodesOfAggregates)
		{
			AggregatesOfGraphNodes = aggregatesOfGraphNodes;
			RootGraphNodesOfAggregates = rootGraphNodesOfAggregates;
			NumAggregates = rootGraphNodesOfAggregates.Length;

			Validate();
		}

		/// <summary>
		/// Contains the aggregate ID assigned to each graph node. This ID is 0, 1, 2, ... 
		/// If a node is not assigned to any aggregate, the value -1 is stored here.
		/// </summary>
		public int[] AggregatesOfGraphNodes { get; }

		public int[] RootGraphNodesOfAggregates { get; }

		public int NumAggregates { get; }

		public int NumGraphNodes => AggregatesOfGraphNodes.Length;

		public CsrMatrix BuildAggregateMembershipMatrix()
		{
			int numGraphNodes = AggregatesOfGraphNodes.Length;

			// Some graph nodes may not be assigned to an aggregate, since they were not connected to another graph node in the first place.
			// For these, the stored aggregate ID is -1. They must be skiped
			int numAggregatedGraphNodes = 0;
			for (int i = 0; i < numGraphNodes; ++i)
			{
				if (AggregatesOfGraphNodes[i] != -1)
				{
					++numAggregatedGraphNodes;
				}
			}

			var values = new double[numAggregatedGraphNodes];
			var columnIndices = new int[numAggregatedGraphNodes];
			var rowOffsets = new int[numGraphNodes + 1];

			int nnz = 0;
			for (int i = 0; i < numGraphNodes; ++i)
			{
				int aggregateID = AggregatesOfGraphNodes[i];
				if (aggregateID != -1)
				{
					values[nnz] = 1.0;
					columnIndices[nnz] = aggregateID;
					++nnz;
				}

				rowOffsets[i + 1] = nnz; // Empty rows retain the cumulative nonzero count.
			}

			return CsrMatrix.CreateFromArrays(numGraphNodes, NumAggregates, values, columnIndices, rowOffsets, false);
		}

		public int[][] GatherGraphNodesPerAggregate()
		{
			var graphNodesPerAggregate = new List<int>[NumAggregates];

			int numNodesPerAggregateEstimate = NumGraphNodes / NumAggregates + 1;
			for (int aggregate = 0; aggregate < NumAggregates; ++aggregate)
			{
				graphNodesPerAggregate[aggregate] = new List<int>(numNodesPerAggregateEstimate);
			}

			for (int n = 0; n < AggregatesOfGraphNodes.Length; ++n)
			{
				int aggregateID = AggregatesOfGraphNodes[n];
				if (aggregateID != -1)
				{
					graphNodesPerAggregate[aggregateID].Add(n);
				}
			}

			var result = new int[NumAggregates][];
			for (int aggregateID = 0; aggregateID < NumAggregates; ++aggregateID)
			{
				result[aggregateID] = graphNodesPerAggregate[aggregateID].ToArray();
			}

			return result;
		}

		[Conditional("DEBUG")]
		private void Validate()
		{
			for (int i = 0; i < AggregatesOfGraphNodes.Length; ++i)
			{
				int aggregate = AggregatesOfGraphNodes[i];
				if (aggregate < -1 || aggregate >= NumAggregates)
				{
					throw new ArgumentException("Fine DOF has an invalid aggregate index.");
				}
			}
		}
	}
}
