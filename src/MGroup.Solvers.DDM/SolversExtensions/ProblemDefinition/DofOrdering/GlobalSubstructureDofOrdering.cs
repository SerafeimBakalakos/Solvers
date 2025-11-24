namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.DofOrdering
{
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class GlobalSubstructureDofOrdering : ISubstructureDofOrdering
	{
		private readonly ISubstructure substructure;

		private Dictionary<ISuperElement, int[]> elementToSubdomainDofs = new Dictionary<ISuperElement, int[]>();

		public GlobalSubstructureDofOrdering(ISubstructure substructure)
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
			bool isStored = elementToSubdomainDofs.TryGetValue(superElement, out int[] substructureDofIndices);
			if (!isStored)
			{
				substructureDofIndices = MapDofs(superElement);
				elementToSubdomainDofs[superElement] = substructureDofIndices;
			}

			int[] elementDofIndices = Utilities.Range(0, substructureDofIndices.Length);
			return (elementDofIndices, substructureDofIndices);
		}

		public void PrepareDofMaps()
		{
			elementToSubdomainDofs = new Dictionary<ISuperElement, int[]>();
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				elementToSubdomainDofs[element] = MapDofs(element);
			}
		}

		public void Reorder(IReorderingAlgorithm reorderingAlgorithm)
		{
			throw new NotImplementedException();
			//var pattern = SparsityPatternSymmetric.CreateEmpty(NumDofs);
			//foreach (var element in substructure.EnumerateSuperElements())
			//{
			//	(var elementDofIndices, var subdomainDofIndices) = MapDofsElementToSubstructure(element);

			//	//TODO: ISubdomainFreeDofOrdering could perhaps return whether the subdomainDofIndices are sorted or not.
			//	pattern.ConnectIndices(subdomainDofIndices, false);
			//}

			//(var permutation, var oldToNew) = reorderingAlgorithm.FindPermutation(pattern);
			//Dofs.Reorder(permutation, oldToNew);
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
	}
}
