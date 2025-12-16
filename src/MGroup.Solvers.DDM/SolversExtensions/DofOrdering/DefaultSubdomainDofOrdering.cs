namespace MGroup.Solvers.DDM.SolversExtensions.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DefaultSubdomainDofOrdering : ISubdomainDofOrdering_v2
	{
		private readonly ISubdomain_v2 subdomain;
		private readonly IReorderingAlgorithm? reorderingAlgorithm;
		private Dictionary<int, (int[] element, int[] subdomain)> elementToSubdomainDofIndices = new Dictionary<int, (int[], int[])>();

		public DefaultSubdomainDofOrdering(ISubdomain_v2 subdomain, IReorderingAlgorithm? reorderingAlgorithm)
		{
			this.subdomain = subdomain;
			this.reorderingAlgorithm = reorderingAlgorithm;
		}

		public IntDofTable Dofs { get; private set; }

		public int NumDofs { get; private set; }

		/// <summary>
		/// </summary>
		/// <param name="element"></param>
		/// <remarks>
		/// Assumes that all dofs in <paramref name="element"/> also exist in the subdomain.
		/// </remarks>
		/// <returns></returns>
		public (int[] elementDofIndices, int[] subdomainDofIndices) MapDofsElementToSubdomain(ISuperElement element)
		{
			bool isStored = elementToSubdomainDofIndices.TryGetValue(element.ID, out (int[] element, int[] subdomain) dofIndices);
			if (!isStored)
			{
				dofIndices = MapDofs(element);
				elementToSubdomainDofIndices[element.ID] = dofIndices;
			}

			return dofIndices;
		}

		public void OrderDofs()
		{
			Dofs = subdomain.OrderDofs();
			NumDofs = Dofs.NumEntries;
			if (reorderingAlgorithm != null)
			{
				ReorderDofs(reorderingAlgorithm);
			}
		}

		public void PrepareDofMaps()
		{
			elementToSubdomainDofIndices = new Dictionary<int, (int[], int[])>();
			foreach (ISuperElement element in subdomain.EnumerateSuperElements())
			{
				elementToSubdomainDofIndices[element.ID] = MapDofs(element);
			}
		}

		private (int[] elementDofIndices, int[] subdomainDofIndices) MapDofs(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs();
			int numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var elementDofIndices = new List<int>(numElementDofs);
			var subdomainDofIndices = new List<int>(numElementDofs);
			foreach ((int nodeID, int dofID, int elementDofIdx) in elementDofs)
			{
				if (Dofs.TryGetValue(nodeID, dofID, out int subdomainDofIdx))
				{
					elementDofIndices.Add(elementDofIdx);
					subdomainDofIndices.Add(subdomainDofIdx);
				}
			}

			return (elementDofIndices.ToArray(), subdomainDofIndices.ToArray());
		}

		private void ReorderDofs(IReorderingAlgorithm reorderingAlgorithm)
		{
			var pattern = SparsityPatternSymmetric.CreateEmpty(NumDofs);
			foreach (ISuperElement element in subdomain.EnumerateSuperElements())
			{
				(int[] elementDofIndices, int[] subdomainDofIndices) = MapDofsElementToSubdomain(element);

				//TODO: ISubdomainFreeDofOrdering could perhaps return whether the subdomainDofIndices are sorted or not.
				pattern.ConnectIndices(subdomainDofIndices, false);
			}

			(int[] permutation, bool oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			Dofs.Reorder(permutation, oldToNew);
		}
	}
}
