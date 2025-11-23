namespace MGroup.SolverExtensions.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.SolverExtensions.LinearSystem;
	using MGroup.Solvers;

	public class SubstructureDofOrderingGeneral : ISubstructureDofOrdering
	{
		private readonly ISubstructure substructure;

		public SubstructureDofOrderingGeneral(ISubstructure substructure, int numFreeDofs, IntDofTable subdomainFreeDofs)
		{
			this.substructure = substructure;
			this.NumDofs = numFreeDofs;
			this.Dofs = subdomainFreeDofs;
		}

		public IntDofTable Dofs { get; }

		public int NumDofs { get; }

		/// <summary>
		/// </summary>
		/// <param name="superElement"></param>
		/// <remarks>
		/// Assumes that all dofs in <paramref name="superElement"/> also exist in the substructure.
		/// </remarks>
		/// <returns></returns>
		public (int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement)
		{
			IntDofTable elementDofs = superElement.GetDofs(); // TODO: Can be optimized by also returning the count
			int numElementDofs = elementDofs.NumEntries;

			var elementDofIndices = new int[numElementDofs]; // This will be a contiguous array: [0, 1, 2, ..., n]
			var substructureDofIndices = new int[numElementDofs];

			foreach ((int nodeID, int dofID, int elementDofIdx) in elementDofs)
			{
				elementDofIndices[elementDofIdx] = elementDofIdx;
				substructureDofIndices[elementDofIdx] = this.Dofs[nodeID, dofID];
			}

			return (elementDofIndices, substructureDofIndices);
		}

		public void Reorder(IReorderingAlgorithm reorderingAlgorithm)
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
