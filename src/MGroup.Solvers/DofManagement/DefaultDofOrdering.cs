namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;

	public class DefaultDofOrdering : IDofOrderingStrategy_v2
	{
		private readonly bool sortNodes;
		private readonly bool sortDofs;

		public DefaultDofOrdering(bool sortNodes = true, bool sortDofs = true)
		{
			this.sortNodes = sortNodes;
			this.sortDofs = sortDofs;
		}

		public IntDofTable OrderDomainDofs(ISubdomain_v2 domain, Func<int, IntDofTable> getElementDofs)
		{
			// Gather dofs from elements
			var dofsAtNodes = new Dictionary<int, HashSet<int>>();
			//var dofsAtNodes = new Dictionary<int, HashSet<int>>(domain.EnumerateNodes().Count());
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				IntDofTable elementDofs = getElementDofs(element.ID);
				foreach ((int nodeID, int dofID, _) in elementDofs)
				{
					bool nodeExists = dofsAtNodes.TryGetValue(nodeID, out var dofsOfNode);
					if (!nodeExists)
					{
						dofsOfNode = new HashSet<int>();
						dofsAtNodes[nodeID] = dofsOfNode;
					}

					dofsOfNode.Add(dofID);
				}
			}

			// Order the dofs. A lot of duplicate code, but performance matters here.
			if (sortNodes)
			{
				return SortAndOrderGatheredDofs(dofsAtNodes);
			}
			else
			{
				return OrderGatheredDofs(dofsAtNodes);

			}
		}

		private IntDofTable OrderGatheredDofs(Dictionary<int, HashSet<int>> uniqueDofs)
		{
			var result = new IntDofTable();
			int dofIdx = -1;
			if (sortDofs)
			{
				foreach (int nodeID in uniqueDofs.Keys)
				{
					int[] dofs = uniqueDofs[nodeID].ToArray();
					Array.Sort(dofs);
					foreach (int dofID in dofs)
					{
						result.TryAdd(nodeID, dofID, ++dofIdx);
					}
				}
			}
			else
			{
				foreach (int nodeID in uniqueDofs.Keys)
				{
					foreach (int dofID in uniqueDofs[nodeID])
					{
						result.TryAdd(nodeID, dofID, ++dofIdx);
					}
				}
			}
				
			return result;
		}

		private IntDofTable SortAndOrderGatheredDofs(Dictionary<int, HashSet<int>> uniqueDofs)
		{
			int[] nodes = uniqueDofs.Keys.ToArray();
			Array.Sort(nodes);
			var result = new IntDofTable();
			int dofIdx = -1;
			if (sortDofs)
			{
				foreach (int nodeID in nodes)
				{
					int[] dofs = uniqueDofs[nodeID].ToArray();
					Array.Sort(dofs);
					foreach (int dofID in dofs)
					{
						result.TryAdd(nodeID, dofID, ++dofIdx);
					}
				}
			}
			else
			{
				foreach (int nodeID in nodes)
				{
					foreach (int dofID in uniqueDofs[nodeID])
					{
						result.TryAdd(nodeID, dofID, ++dofIdx);
					}
				}
			}

			return result;
		}
	}
}
