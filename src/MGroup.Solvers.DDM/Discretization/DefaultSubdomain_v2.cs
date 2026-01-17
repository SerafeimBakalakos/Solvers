namespace MGroup.Solvers.DDM.Discretization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering_v2;

	public class DefaultSubdomain_v2 : ISubdomain_v2
	{
		private readonly bool cacheElementDofs;
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;
		private readonly IModel_v2 model;

		private Dictionary<int, INode> nodes = new Dictionary<int, INode>();
		private Dictionary<int, DefaultElement> elements = new Dictionary<int, DefaultElement>();

		public DefaultSubdomain_v2(int id, IModel_v2 model, ConstrainedDofLocator constrainedDofLocator, IElementMatrixProvider elementMatrixProvider, bool cacheElementDofs = true)
		{
			ID = id;
			this.model = model;
			this.constrainedDofLocator = constrainedDofLocator;
			this.elementMatrixProvider = elementMatrixProvider;
			this.cacheElementDofs = cacheElementDofs;
		}

		public int ID { get; }

		public int NumElements => elements.Count;

		public int NumNodes => nodes.Count;

		public void AddElement(IElementType element)
		{
			if (cacheElementDofs)
			{
				elements[element.ID] = new DefaultElementCaching(element, model, constrainedDofLocator, elementMatrixProvider);
			}
			else
			{
				elements[element.ID] = new DefaultElement(element, model, constrainedDofLocator, elementMatrixProvider);
			}

			foreach (INode node in element.Nodes)
			{
				nodes.TryAdd(node.ID, node);
			}
		}

		public IEnumerable<INode> EnumerateNodes() => nodes.Values;

		public IEnumerable<ISuperElement> EnumerateElements() => elements.Values;

		public IEnumerable<INodalDirichletBoundaryCondition<IDofType>> FindDirichletBCs()
		{
			return model.EnumerateBoundaryConditions()
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(elements.Values.Select(e => e.ElementEntity)))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
		}

		public ISuperElement GetElement(int elementID) => elements[elementID];

		public INode GetNode(int nodeID) => throw new NotImplementedException();
	}
}
