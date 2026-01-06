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
	using MGroup.Solvers;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;

	public class DefaultSubdomain_temp : ISubdomain_v2
	{
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;
		private readonly IModel_v2 model;

		private SortedSet<INode> nodes = new SortedSet<INode>(Comparer<INode>.Create((n1, n2) => n1.ID.CompareTo(n2.ID)));
		private Dictionary<int, DefaultElement_temp> elements = new Dictionary<int, DefaultElement_temp>();

		public DefaultSubdomain_temp(int id, IModel_v2 model, ConstrainedDofLocator constrainedDofLocator, IElementMatrixProvider elementMatrixProvider)
		{
			ID = id;
			this.model = model;
			this.constrainedDofLocator = constrainedDofLocator;
			this.elementMatrixProvider = elementMatrixProvider;
		}

		public int ID { get; }

		public void AddElement(IElementType element)
		{
			elements[element.ID] = new DefaultElement_temp(element, model, constrainedDofLocator, elementMatrixProvider);
			foreach (INode node in element.Nodes)
			{
				nodes.Add(node);
			}
		}

		public IEnumerable<INode> EnumerateNodes() => nodes;

		public IEnumerable<ISuperElement> EnumerateElements() => elements.Values;

		public ISuperElement GetElement(int elementID) => elements[elementID];

		public IntDofTable OrderDofs_temp()
		{
			var freeDofOrderer = new DefaultFreeDofOrderer_v2(model);
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> dirichletBCs = FindDiricletBCs();
			return freeDofOrderer.OrderFreeDofs(elements.Values.Select(e => e.ElementEntity), nodes, dirichletBCs);
		}

		public void PrepareDofs()
		{
			//This works only for non-overlapping DDMs
			foreach (DefaultElement_temp element in elements.Values)
			{
				element.PrepareDofs();
			}
		}

		public IEnumerable<INodalDirichletBoundaryCondition<IDofType>> FindDiricletBCs()
		{
			return model.EnumerateBoundaryConditions()
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(elements.Values.Select(e => e.ElementEntity)))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
		}
	}
}
