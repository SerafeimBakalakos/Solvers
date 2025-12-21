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

	public class DefaultSubdomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly IElementMatrixProvider elementMatrixProvider;

		private SortedSet<INode> nodes = new SortedSet<INode>(
			Comparer<INode>.Create((n1, n2) => n1.ID.CompareTo(n2.ID)));
		private SortedSet<IElementType> elements = new SortedSet<IElementType>(
			Comparer<IElementType>.Create((e1, e2) => e1.ID.CompareTo(e2.ID)));

		public DefaultSubdomain_temp(int id, IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.ID = id;
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
		}

		public int ID { get; }

		public void AddElement(IElementType element)
		{
			elements.Add(element);
			foreach (INode node in element.Nodes)
			{
				nodes.Add(node);
			}
		}

		public IEnumerable<INode> EnumerateNodes() => nodes;

		public IEnumerable<ISuperElement> EnumerateElements() 
			=> elements.Select(e => new DefaultElement_temp(e, model.DofTypes, elementMatrixProvider));

		public IntDofTable OrderDofs()
		{
			ActiveDofs activeDofs = model.DofTypes;
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> dirichletBCs = FindDiricletBCs();
			return FullDomain_temp.OrderFreeDofs(activeDofs, elements, nodes, dirichletBCs);
		}

		public IEnumerable<INodalDirichletBoundaryCondition<IDofType>> FindDiricletBCs()
		{
			return model.EnumerateBoundaryConditions()
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(elements))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
		}
	}
}
