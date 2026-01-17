namespace MGroup.Solvers.DofOrdering_v2
{
	using System.Collections.Generic;
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DiscretizationExtensions;

	public class MonolithicDomainDofManagerCaching : MonolithicDomainDofManager
	{
		private Dictionary<int, int[]> elementToDomainDofIndices;

		public MonolithicDomainDofManagerCaching(ISubdomain_v2 domain, IDofOrderingStrategy_v2 orderingStrategy, IReorderingAlgorithm? reorderingAlgorithm)
			: base(domain, orderingStrategy, reorderingAlgorithm)
		{
		}

		public override int[] MapDofsElementToDomain(ISuperElement element) => elementToDomainDofIndices[element.ID];

		public override void PrepareDofs()
		{
			base.PrepareDofs();

			// Element-to-domain dof maps
			elementToDomainDofIndices = new Dictionary<int, int[]>();
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				elementToDomainDofIndices[element.ID] = MapElementDofs(element);
			}
		}
	}
}
