
namespace MGroup.Solvers.Discretization
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
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.DofOrdering_v2;

	public class GlobalDomain : IDomain
	{
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;
		private readonly Dictionary<int, DefaultElement> elements;

		public GlobalDomain(IModel_v2 model, IElementMatrixProvider elementMatrixProvider, bool cacheElementDofs = true)
		{
			this.Model = model;
			this.elementMatrixProvider = elementMatrixProvider;

			constrainedDofLocator = new ConstrainedDofLocator(model);
			
			elements = new Dictionary<int, DefaultElement>();
			if (cacheElementDofs)
			{
				foreach (IElementType element in model.EnumerateElements())
				{
					elements[element.ID] = new DefaultElementCaching(element, Model, constrainedDofLocator, elementMatrixProvider);
				}
			}
			else
			{
				foreach (IElementType element in model.EnumerateElements())
				{
					elements[element.ID] = new DefaultElement(element, Model, constrainedDofLocator, elementMatrixProvider);
				}
			}
		}

		public IModel_v2 Model { get; }

		public int NumElements => Model.NumElements;

		public int NumNodes => Model.NumNodes;

		public IEnumerable<INode> EnumerateNodes() => Model.EnumerateNodes();

		public IEnumerable<ISuperElement> EnumerateElements() => elements.Values;

		public ISuperElement GetElement(int id) => elements[id];

		public INode GetNode(int nodeID) => Model.GetNode(nodeID);
	}
}
