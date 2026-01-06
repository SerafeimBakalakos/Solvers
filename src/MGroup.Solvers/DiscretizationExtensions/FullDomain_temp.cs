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
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;

	public class FullDomain_temp : ISubdomain_v2
	{
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;
		private readonly Dictionary<int, DefaultElement_temp> elements;

		public FullDomain_temp(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.Model = model;
			this.elementMatrixProvider = elementMatrixProvider;
			this.ID = 0;

			constrainedDofLocator = new ConstrainedDofLocator(model);
			elements = new Dictionary<int, DefaultElement_temp>();
			foreach (IElementType element in model.EnumerateElements())
			{
				elements[element.ID] = new DefaultElement_temp(element, Model, constrainedDofLocator, elementMatrixProvider);
			}
		}

		public int ID { get; }

		public IModel_v2 Model { get; }

		public IEnumerable<INode> EnumerateNodes() => Model.EnumerateNodes();

		public IEnumerable<ISuperElement> EnumerateElements() => elements.Values;

		public ISuperElement GetElement(int id) => elements[id];

		public IntDofTable OrderDofs_temp()
		{
			var freeDofOrderer = new DefaultFreeDofOrderer_v2(Model);
			return freeDofOrderer.OrderFreeDofs(Model.EnumerateElements(), Model.EnumerateNodes(), Model.GetDirichletBCs());
		}
	}
}
