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
	using MGroup.Solvers.DofOrdering.Reordering;

	public class FullDomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly IElementMatrixProvider elementMatrixProvider;

		public FullDomain_temp(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
			this.ID = 0;
		}

		public int ID { get; }

		public IEnumerable<INode> EnumerateNodes() => model.EnumerateNodes();

		public IEnumerable<ISuperElement> EnumerateElements()
			=> model.EnumerateElements().Select(e => new DefaultElement_temp(e, model.DofTypes, elementMatrixProvider));

		public IntDofTable OrderDofs()
		{
			var freeDofOrderer = new DefaultFreeDofOrderer_v2(model);
			return freeDofOrderer.OrderFreeDofs(model.EnumerateElements(), model.EnumerateNodes(), model.GetDirichletBCs());
		}
	}
}
