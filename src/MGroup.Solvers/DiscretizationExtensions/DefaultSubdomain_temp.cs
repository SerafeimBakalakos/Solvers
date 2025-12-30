using MGroup.Solvers.DiscretizationExtensions;

namespace MGroup.Solvers.DiscretizationExtensions
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
	using MGroup.Solvers.DofOrdering.Reordering;

	public class DefaultSubdomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly IElementMatrixProvider elementMatrixProvider;

		private SortedSet<INode> nodes = new SortedSet<INode>(
			Comparer<INode>.Create((n1, n2) => n1.ID.CompareTo(n2.ID)));
		private Dictionary<int, DefaultElement_temp> elements = new Dictionary<int, DefaultElement_temp>();

		public DefaultSubdomain_temp(int id, IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.ID = id;
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
		}

		public int ID { get; }

		public void AddElement(IElementType element)
		{
			elements[element.ID] = new DefaultElement_temp(element, model.DofTypes, elementMatrixProvider);
			foreach (INode node in element.Nodes)
			{
				nodes.Add(node);
			}
		}

		public IEnumerable<INode> EnumerateNodes() => nodes;

		public IEnumerable<ISuperElement> EnumerateElements() => elements.Values;

		public ISuperElement GetElement(int elementID) => elements[elementID];

		public IntDofTable OrderDofs()
		{
			var freeDofOrderer = new DefaultFreeDofOrderer_v2(model);
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> dirichletBCs = FindDiricletBCs();
			return freeDofOrderer.OrderFreeDofs(elements.Values.Select(e => e.ElementEntity), nodes, dirichletBCs);
		}

		public IEnumerable<INodalDirichletBoundaryCondition<IDofType>> FindDiricletBCs()
		{
			return model.EnumerateBoundaryConditions()
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(elements.Values.Select(e => e.ElementEntity)))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
		}
	}
}
