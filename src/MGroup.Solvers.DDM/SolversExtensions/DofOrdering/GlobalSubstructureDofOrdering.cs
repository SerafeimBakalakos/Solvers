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
			var isStored = elementToSubstructureDofIndices.TryGetValue(superElement.ID, out var dofIndices);
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
			foreach (var element in substructure.EnumerateSuperElements())
			{
				elementToSubstructureDofIndices[element.ID] = MapDofs(element);
			}
		}

		private (int[] elementDofIndices, int[] substructureDofIndices) MapDofs(ISuperElement superElement)
		{
			var elementDofs = superElement.GetDofs();
			var numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var elementDofIndices = new List<int>(numElementDofs);
			var substructureDofIndices = new List<int>(numElementDofs);
			foreach ((var nodeID, var dofID, var elementDofIdx) in elementDofs)
			{
				if (Dofs.TryGetValue(nodeID, dofID, out var substructureDofIdx))
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
			foreach (var element in substructure.EnumerateSuperElements())
			{
				(var elementDofIndices, var subdomainDofIndices) = MapDofsElementToSubstructure(element);

				//TODO: ISubdomainFreeDofOrdering could perhaps return whether the subdomainDofIndices are sorted or not.
				pattern.ConnectIndices(subdomainDofIndices, false);
			}

			(var permutation, var oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			Dofs.Reorder(permutation, oldToNew);
		}
	}
}
