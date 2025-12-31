namespace MGroup.Solvers.MatrixFree.Dofs
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.LinearAlgebraExtensions;

	public class DofOrderingDistributed
	{
		private readonly IComputeEnvironment environment;
		private readonly ISubdomain_v2 domain;
		private readonly IElementPartition partition;
		private readonly FreeDofSelector_temp freeDofSelector;

		public DofOrderingDistributed(IComputeEnvironment environment, ISubdomain_v2 domain, IElementPartition partition, FreeDofSelector_temp freeDofSelector)
		{
			this.environment = environment;
			this.domain = domain;
			this.partition = partition;
			this.freeDofSelector = freeDofSelector;
		}

		public DistributedOverlappingIndexer CreateIndexer()
		{
			ConcurrentDictionary<int, Dictionary<int, SortedDofSet>> commonDofsBetweenElements = FindAllCommonDofs();
			var indexer = new DistributedOverlappingIndexer(environment);
			indexer.Initialize(elementID => InitializeIndexer(elementID, commonDofsBetweenElements[elementID]));
			return indexer;
		}

		private ConcurrentDictionary<int, Dictionary<int, SortedDofSet>> FindAllCommonDofs()
		{
			var commonDofsBetweenElements = new ConcurrentDictionary<int, Dictionary<int, SortedDofSet>>();

			// Find all dofs of each subdomain at the common nodes.
			environment.DoPerNode(subdomainID =>
			{
				commonDofsBetweenElements[subdomainID] = FindCommonDofsOfElement(subdomainID);
			});

			// Send these dofs to the corresponding neighbors and receive theirs.
			Dictionary<int, AllToAllNodeData<int>> transferDataPerElement = environment.CalcNodeData(elementID =>
			{
				var transferData = new AllToAllNodeData<int>();
				transferData.sendValues = new ConcurrentDictionary<int, int[]>();
				foreach (int neighborID in partition.GetNeighborsOfElement(elementID))
				{
					SortedDofSet commonDofs = commonDofsBetweenElements[elementID][neighborID];

					//TODOMPI: Serialization & deserialization should be done by the environment, if necessary.
					transferData.sendValues[neighborID] = commonDofs.Serialize();
				}

				// No buffers for receive values yet, since their lengths are unknown. 
				// Let the environment create them, by using extra communication.
				transferData.recvValues = new ConcurrentDictionary<int, int[]>();
				return transferData;
			});
			environment.NeighborhoodAllToAll(transferDataPerElement, false);

			// Find the intersection between the dofs of an element and the ones received by its neighbor.
			environment.DoPerNode(elementID =>
			{
				AllToAllNodeData<int> transferData = transferDataPerElement[elementID];
				foreach (int neighborID in partition.GetNeighborsOfElement(elementID))
				{
					SortedDofSet receivedDofs = SortedDofSet.Deserialize(transferData.recvValues[neighborID]);
					commonDofsBetweenElements[elementID][neighborID] =
						commonDofsBetweenElements[elementID][neighborID].IntersectionWith(receivedDofs);
				}
			});

			return commonDofsBetweenElements;
		}

		private Dictionary<int, SortedDofSet> FindCommonDofsOfElement(int elementID)
		{
			ISuperElement element = domain.GetElement(elementID);
			IntDofTable elementDofs = freeDofSelector.GetFreeDofsOfElement(element);

			var commonDofsOfElement = new Dictionary<int, SortedDofSet>();
			foreach (int neighborID in partition.GetNeighborsOfElement(elementID))
			{
				var dofSet = new SortedDofSet();
				foreach (int nodeID in partition.GetCommonNodesOfElements(elementID, neighborID))
				{
					dofSet.AddDofs(nodeID, elementDofs.GetColumnsOfRow(nodeID));
				}
				commonDofsOfElement[neighborID] = dofSet;
			}
			return commonDofsOfElement;
		}

		private LocalIndexerDto InitializeIndexer(int elementID, Dictionary<int, SortedDofSet> commonDofsWithNeighbors)
		{
			ISuperElement element = domain.GetElement(elementID);
			IntDofTable elementDofs = freeDofSelector.GetFreeDofsOfElement(element);

			var allCommonDofIndices = new Dictionary<int, int[]>();
			foreach (int neighborID in partition.GetNeighborsOfElement(elementID))
			{
				SortedDofSet commonDofs = commonDofsWithNeighbors[neighborID];
				var commonDofIndices = new List<int>(commonDofs.Count());
				foreach ((int nodeID, int dofID) in commonDofs.EnumerateOrderedNodesDofs())
				{
					//TODO: It would be faster to iterate each node and then its dofs. Same for DofTable. 
					//		Even better let DofTable take DofSet as argument and return the indices.
					commonDofIndices.Add(elementDofs[nodeID, dofID]);
					
				}
				allCommonDofIndices[neighborID] = commonDofIndices.ToArray();
			}

			return LocalIndexerDto.CreateWithNewContent(elementDofs.NumEntries, allCommonDofIndices);
		}

	}
}
