namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class SubstructureDofOrderingGeneral : ISubstructureDofOrdering
	{
		private readonly ISubstructure substructure;

		public SubstructureDofOrderingGeneral(ISubstructure substructure)
		{
			this.substructure = substructure;
			Dofs = substructure.OrderDofs();
			NumDofs = Dofs.NumEntries;
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
			IntDofTable elementDofs = superElement.GetDofs();
			var numElementDofs = elementDofs.NumEntries;

			var elementDofIndices = new int[numElementDofs]; // This will be a contiguous array: [0, 1, 2, ..., n]
			var substructureDofIndices = new int[numElementDofs];

			foreach ((var nodeID, var dofID, var elementDofIdx) in elementDofs)
			{
				elementDofIndices[elementDofIdx] = elementDofIdx;
				substructureDofIndices[elementDofIdx] = Dofs[nodeID, dofID];
			}

			return (elementDofIndices, substructureDofIndices);
		}

		public void Reorder(IReorderingAlgorithm reorderingAlgorithm)
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
