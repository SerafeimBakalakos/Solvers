namespace MGroup.Solvers.DofOrdering_v2
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DofOrdering_v2;

	public class MonolithicDomainDofManager : IMonolithicDofManager
	{
		protected readonly IDomain domain;
		private readonly IDofOrderingStrategy_v2 orderingStrategy;
		private readonly IReorderingAlgorithm? reorderingAlgorithm;

		public MonolithicDomainDofManager(IDomain domain, IDofOrderingStrategy_v2 orderingStrategy, IReorderingAlgorithm? reorderingAlgorithm)
		{
			this.domain = domain;
			this.orderingStrategy = orderingStrategy;
			this.reorderingAlgorithm = reorderingAlgorithm;
		}

		public IntDofTable DomainDofOrder { get; private set; }

		public IntDofTable GetElementDofs(int elementID) => domain.GetElement(elementID).GetDofs();

		public int NumDomainDofs { get; private set; }

		public virtual int[] MapDofsElementToDomain(ISuperElement element) => MapElementDofs(element);

		public virtual void PrepareDofs()
		{
			// Domain dofs
			DomainDofOrder = orderingStrategy.OrderDomainDofs(domain, GetElementDofs);
			NumDomainDofs = DomainDofOrder.NumEntries;
			
			// Reordering
			if (reorderingAlgorithm != null)
			{
				ReorderDofs(reorderingAlgorithm);
			}
		}

		protected int[] MapElementDofs(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs();
			int numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var map = new int[numElementDofs];
			foreach ((int nodeID, int dofID, int elementDofIdx) in elementDofs)
			{
				int domainDofIdx = DomainDofOrder[nodeID, dofID]; // If the element has dofs that do not exist in the domain, something has gone wrong. Let it throw an exception.
				map[elementDofIdx] = domainDofIdx;
			}

			return map;
		}

		private void OrderDomainDofs()
		{
			
		}

		private void ReorderDofs(IReorderingAlgorithm reorderingAlgorithm)
		{
			var pattern = SparsityPatternSymmetric.CreateEmpty(NumDomainDofs);
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				int[] elementToDomainDofs = MapElementDofs(element);

				//TODO: This object could perhaps return whether the subdomainDofIndices are sorted or not.
				pattern.ConnectIndices(elementToDomainDofs, false);
			}

			(int[] permutation, bool oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			DomainDofOrder.Reorder(permutation, oldToNew);
		}
	}
}
