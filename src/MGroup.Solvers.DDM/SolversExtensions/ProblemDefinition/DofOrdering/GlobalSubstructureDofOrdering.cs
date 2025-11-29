namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class GlobalSubstructureDofOrdering : ISubstructureDofOrdering
	{
		private readonly ISubstructure substructure;
		private readonly IReorderingAlgorithm? reorderingAlgorithm;
		private Dictionary<ISuperElement, int[]> elementToSubdomainDofs = new Dictionary<ISuperElement, int[]>();

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
			bool isStored = elementToSubdomainDofs.TryGetValue(superElement, out int[] substructureDofIndices);
			if (!isStored)
			{
				substructureDofIndices = MapDofs(superElement);
				elementToSubdomainDofs[superElement] = substructureDofIndices;
			}

			int[] elementDofIndices = Utilities.Range(0, substructureDofIndices.Length);
			return (elementDofIndices, substructureDofIndices);
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
			elementToSubdomainDofs = new Dictionary<ISuperElement, int[]>();
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				elementToSubdomainDofs[element] = MapDofs(element);
			}
		}

		private int[] MapDofs(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs();
			var numElementDofs = elementDofs.NumEntries; //TODO: Optimize this

			var substructureDofIndices = new int[numElementDofs];
			foreach ((var nodeID, var dofID, var elementDofIdx) in elementDofs)
			{
				substructureDofIndices[elementDofIdx] = Dofs[nodeID, dofID];
			}

			return substructureDofIndices;
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
