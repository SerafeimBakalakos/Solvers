namespace MGroup.Solvers.DDM.Discretization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering_v2;

	public class DecomposedNonOverlappingDomain : IDomain
	{
		private readonly IModel_v2 model;
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly Func<int, int> getSubdomainOfElement;
		private readonly DefaultSubdomain_v2[] subdomains;

		public DecomposedNonOverlappingDomain(IModel_v2 model, IElementMatrixProvider elementMatrixProvider, int numSubdomains, Func<int, int> getSubdomainOfElement, bool cacheElementDofs = true)
		{
			this.model = model;
			this.constrainedDofLocator = new ConstrainedDofLocator(model);
			this.getSubdomainOfElement = getSubdomainOfElement;

			this.subdomains = new DefaultSubdomain_v2[numSubdomains];
			for (int s = 0; s < numSubdomains; s++)
			{
				this.subdomains[s] = new DefaultSubdomain_v2(s, model, constrainedDofLocator, elementMatrixProvider, cacheElementDofs);
			}

			foreach (IElementType element in model.EnumerateElements())
			{
				int subdomainID = getSubdomainOfElement(element.ID);
				this.subdomains[subdomainID].AddElement(element);
			}

			this.Partition = new SharedMemoryPartition(subdomains);
		}

		public IPartition_v2 Partition { get; }

		public int NumElements => model.NumElements;

		public int NumNodes => model.NumNodes;

		public IEnumerable<ISuperElement> EnumerateElements()
			=> model.EnumerateElements().Select(e => subdomains[getSubdomainOfElement(e.ID)].GetElement(e.ID));

		public IEnumerable<INode> EnumerateNodes() => model.EnumerateNodes();

		public ISuperElement GetElement(int elementID)
			=> subdomains[getSubdomainOfElement(elementID)].GetElement(elementID);

		public INode GetNode(int nodeID) => model.GetNode(nodeID);
	}
}
