namespace MGroup.Solvers.DofOrdering
{
	using System.Collections.Generic;
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DiscretizationExtensions;

	public class MonolithicDomainDofOrdering : ISubdomainDofOrdering_v2
	{
		protected readonly ISubdomain_v2 domain;
		private readonly IReorderingAlgorithm? reorderingAlgorithm;

		public MonolithicDomainDofOrdering(ISubdomain_v2 domain, IReorderingAlgorithm? reorderingAlgorithm)
		{
			this.domain = domain;
			this.reorderingAlgorithm = reorderingAlgorithm;
		}

		public IntDofTable DomainDofs { get; private set; }

		public int NumDofs { get; private set; }

		public virtual int[] MapDofsElementToDomain(ISuperElement element) => MapElementDofs(element);

		public virtual void OrderDofs()
		{
			// Domain dofs
			DomainDofs = domain.OrderDofs_temp();
			NumDofs = DomainDofs.NumEntries;
			
			// Reordering
			if (reorderingAlgorithm != null)
			{
				ReorderDofs(reorderingAlgorithm);
			}
		}

		//public void WriteLocalToGlobalMaps_temp()
		//{
		//	foreach (int elemID in elementToDomainDofIndices.Keys)
		//	{
		//		int[] localToGlobal = elementToDomainDofIndices[elemID];
		//		Debug.Write($"Element {elemID}: local-to-global dofs =");
		//		foreach (int index in localToGlobal)
		//		{
		//			Debug.Write(" ");
		//			Debug.Write(index);
		//		}
		//		Debug.WriteLine("");
		//	}
		//}

		protected int[] MapElementDofs(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs();
			int numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var map = new int[numElementDofs];
			foreach ((int nodeID, int dofID, int elementDofIdx) in elementDofs)
			{
				int domainDofIdx = DomainDofs[nodeID, dofID]; // If the element has dofs that do not exist in the domain, something has gone wrong. Let it throw an exception.
				map[elementDofIdx] = domainDofIdx;
			}

			return map;
		}

		private void ReorderDofs(IReorderingAlgorithm reorderingAlgorithm)
		{
			var pattern = SparsityPatternSymmetric.CreateEmpty(NumDofs);
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				int[] elementToDomainDofs = MapElementDofs(element);

				//TODO: This object could perhaps return whether the subdomainDofIndices are sorted or not.
				pattern.ConnectIndices(elementToDomainDofs, false);
			}

			(int[] permutation, bool oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			DomainDofs.Reorder(permutation, oldToNew);
		}
	}
}
