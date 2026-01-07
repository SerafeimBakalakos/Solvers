namespace MGroup.Solvers.DofOrdering
{
	using System.Collections.Generic;
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DiscretizationExtensions;

	public class MonolithicDomainDofOrderingCaching : MonolithicDomainDofOrdering
	{
		private Dictionary<int, int[]> elementToDomainDofIndices;

		public MonolithicDomainDofOrderingCaching(ISubdomain_v2 domain, IReorderingAlgorithm? reorderingAlgorithm)
			: base(domain, reorderingAlgorithm)
		{
		}

		public override int[] MapDofsElementToDomain(ISuperElement element) => elementToDomainDofIndices[element.ID];

		public override void OrderDofs()
		{
			base.OrderDofs();

			// Element-to-domain dof maps
			elementToDomainDofIndices = new Dictionary<int, int[]>();
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				elementToDomainDofIndices[element.ID] = MapElementDofs(element);
			}
		}
	}
}
