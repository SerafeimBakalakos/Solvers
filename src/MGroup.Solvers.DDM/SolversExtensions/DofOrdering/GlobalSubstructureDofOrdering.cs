namespace MGroup.Solvers.DDM.SolversExtensions.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class GlobalSubstructureDofOrdering : ISubstructureDofOrdering
	{
		private readonly ISubstructure substructure;
		private readonly IReorderingAlgorithm? reorderingAlgorithm;
		private Dictionary<int, (int[] element, int[] substructure)> elementToSubstructureDofIndices = new Dictionary<int, (int[], int[])>();

		public GlobalSubstructureDofOrdering(ISubstructure substructure, IReorderingAlgorithm? reorderingAlgorithm)
		{
			this.substructure = substructure;
			this.reorderingAlgorithm = reorderingAlgorithm;
		}

		public IntDofTable Dofs { get; private set; }

		public int NumDofs { get; private set; }

		/// <summary>
		/// </summary>
		/// <param name="superElement"></param>
		/// <remarks>
		/// Assumes that all dofs in <paramref name="superElement"/> also exist in the substructure.
		/// </remarks>
		/// <returns></returns>
		public (int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement)
		{
			bool isStored = elementToSubstructureDofIndices.TryGetValue(superElement.ID, out (int[] element, int[] substructure) dofIndices);
			if (!isStored)
			{
				dofIndices = MapDofs(superElement);
				elementToSubstructureDofIndices[superElement.ID] = dofIndices;
			}

			return dofIndices;
		}

		public void OrderDofs()
		{
			Dofs = substructure.OrderDofs();
			NumDofs = Dofs.NumEntries;
			if (reorderingAlgorithm != null)
			{
				ReorderDofs(reorderingAlgorithm);
			}
		}

		public void PrepareDofMaps()
		{
			elementToSubstructureDofIndices = new Dictionary<int, (int[], int[])>();
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				elementToSubstructureDofIndices[element.ID] = MapDofs(element);
			}
		}

		private (int[] elementDofIndices, int[] substructureDofIndices) MapDofs(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs();
			int numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var elementDofIndices = new List<int>(numElementDofs);
			var substructureDofIndices = new List<int>(numElementDofs);
			foreach ((int nodeID, int dofID, int elementDofIdx) in elementDofs)
			{
				if (Dofs.TryGetValue(nodeID, dofID, out int substructureDofIdx))
				{
					elementDofIndices.Add(elementDofIdx);
					substructureDofIndices.Add(substructureDofIdx);
				}
			}

			return (elementDofIndices.ToArray(), substructureDofIndices.ToArray());
		}

		private void ReorderDofs(IReorderingAlgorithm reorderingAlgorithm)
		{
			var pattern = SparsityPatternSymmetric.CreateEmpty(NumDofs);
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				(int[] elementDofIndices, int[] subdomainDofIndices) = MapDofsElementToSubstructure(element);

				//TODO: ISubdomainFreeDofOrdering could perhaps return whether the subdomainDofIndices are sorted or not.
				pattern.ConnectIndices(subdomainDofIndices, false);
			}

			(int[] permutation, bool oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			Dofs.Reorder(permutation, oldToNew);
		}
	}
}
