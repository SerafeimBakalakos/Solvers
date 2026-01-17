namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;

	public class DefaultElementPartition : IElementPartition
	{
		private readonly IComputeEnvironment environment;
		private readonly IModel_v2 model;
		private readonly GlobalDomain fullDomain;

		private ConcurrentDictionary<int, ISet<int>> elementNeighbors;

		public DefaultElementPartition(IComputeEnvironment environment, IModel_v2 model, GlobalDomain fullDomain)
		{
			this.environment = environment;
			this.model = model;
			this.fullDomain = fullDomain;
		}

		public IEnumerable<ISuperElement> Elements => fullDomain.EnumerateElements();

		public void FindElementNeighbors()
		{
			elementNeighbors = new ConcurrentDictionary<int, ISet<int>>();
			environment.DoPerNode(elementID =>
			{
				IElementType element = model.GetElement(elementID);
				var neighbors = new HashSet<int>();
				foreach (INode node in element.Nodes)
				{
					neighbors.UnionWith(node.ElementsDictionary.Values.Select(e => e.ID));
				}
				neighbors.Remove(elementID);
				elementNeighbors[elementID]	= neighbors;
			});
		}

		public int FindMultiplicityOfNode(int nodeID) => model.GetNode(nodeID).ElementsDictionary.Count;

		public ISet<int> GetCommonNodesOfElements(int elementID0, int elementID1)
		{
			if (elementNeighbors[elementID0].Contains(elementID1))
			{
				IElementType element0 = model.GetElement(elementID0);
				IElementType element1 = model.GetElement(elementID1);
				var commonNodes = new HashSet<int>(element0.Nodes.Select(n => n.ID));
				commonNodes.IntersectWith(element1.Nodes.Select(n => n.ID));
				return commonNodes;
			}
			else
			{
				return new HashSet<int>();
			}
		}

		public IEnumerable<int> GetElementsOfNode(int nodeID)
			=> model.GetNode(nodeID).ElementsDictionary.Values.Select(e => e.ID);

		public ISet<int> GetNeighborsOfElement(int elementID) => elementNeighbors[elementID];
	}
}
