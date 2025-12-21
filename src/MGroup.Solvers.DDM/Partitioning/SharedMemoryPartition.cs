namespace MGroup.Solvers.DDM.Partitioning
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;

	public class SharedMemoryPartition : IPartition_v2
	{
		private readonly Dictionary<int, ISubdomain_v2> subdomains;
		private readonly Dictionary<int, SortedSet<ISubdomain_v2>> subdomainsOfNodes;

		public SharedMemoryPartition(IEnumerable<ISubdomain_v2> subdomains)
		{
			this.subdomains = new Dictionary<int, ISubdomain_v2>();
			subdomainsOfNodes = new Dictionary<int, SortedSet<ISubdomain_v2>>();
			foreach (ISubdomain_v2 subdomain in subdomains)
			{
				this.subdomains[subdomain.ID] = subdomain;
				foreach (INode node in subdomain.EnumerateNodes())
				{
					bool isStored = subdomainsOfNodes.TryGetValue(node.ID, out SortedSet<ISubdomain_v2> subdomainsOfNode);
					if (!isStored)
					{
						subdomainsOfNode = new SortedSet<ISubdomain_v2>(
							Comparer<ISubdomain_v2>.Create((s1, s2) => s1.ID.CompareTo(s2.ID)));
						subdomainsOfNodes[node.ID] = subdomainsOfNode;
					}

					subdomainsOfNodes[node.ID].Add(subdomain);
				}
			}
		}

		public IEnumerable<ISubdomain_v2> Subdomains => subdomains.Values;

		public bool DoesSubdomainContainNode(int nodeID, int subdomainID)
		{
			ISubdomain_v2 subdomain = subdomains[subdomainID];
			return subdomainsOfNodes[nodeID].Contains(subdomain);
		}

		public IEnumerable<ISubdomain_v2> EnumerateSubdomainsOfNode(INode node) => subdomainsOfNodes[node.ID];

		public ISubdomain_v2 GetSubdomain(int subdomainID) => subdomains[subdomainID];

		public int FindMultiplicityOfNode(int nodeID) => subdomainsOfNodes[nodeID].Count;
	}
}
