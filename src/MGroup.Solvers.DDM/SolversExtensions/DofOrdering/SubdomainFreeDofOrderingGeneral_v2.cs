namespace MGroup.SolverExtensions.DofOrdering
{
	using System.Collections.Generic;
	using System.Linq;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.SolverExtensions.LinearSystem;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.SolversExtensions.LinearSystem;

	/// <summary>
	/// Deals with the free (unconstrained) dofs of a subdomain.
	/// </summary>
	public class SubdomainFreeDofOrderingGeneral_v2 : ISubstructureDofOrdering
	{
		private readonly DefaultSubstructure substructure;
		private readonly ActiveDofs allDofs;

		public SubdomainFreeDofOrderingGeneral_v2(DefaultSubstructure subdstructure, int numFreeDofs, IntDofTable subdomainFreeDofs, 
			ActiveDofs allDofs)
		{
			this.substructure = subdstructure;
			this.NumDofs = numFreeDofs;
			this.Dofs = subdomainFreeDofs;
			this.allDofs = allDofs;
		}

		public IntDofTable Dofs { get; }

		public int NumDofs { get; }

		public (int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement)
		{
			var element = ((DefaultElement)superElement).ElementEntity;

			IReadOnlyList<INode> elementNodes = element.DofEnumerator.GetNodesForMatrixAssembly(element);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = element.DofEnumerator.GetDofTypesForMatrixAssembly(element);

			// Count the dof superset (free and constrained) to allocate enough memory and avoid resizing
			int allElementDofs = 0;
			for (int i = 0; i < elementNodes.Count; ++i) allElementDofs += elementDofs[i].Count;
			var elementDofIndices = new List<int>(allElementDofs);
			var subdomainDofIndices = new List<int>(allElementDofs);

			int elementDofIdx = 0;
			for (int nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				for (int dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					int dofID = allDofs.GetIdOfDof(elementDofs[nodeIdx][dofIdx]);
					bool isFree = Dofs.TryGetValue(elementNodes[nodeIdx].ID, dofID, out int subdomainDofIdx);
					if (isFree)
					{
						elementDofIndices.Add(elementDofIdx);
						subdomainDofIndices.Add(subdomainDofIdx);
					}

					++elementDofIdx; // This must be incremented for constrained dofs as well
				}
			}

			return (elementDofIndices.ToArray(), subdomainDofIndices.ToArray());
		}

		public void Reorder(IReorderingAlgorithm reorderingAlgorithm) => throw new NotImplementedException("The code in SubstructureDofOrderingGeneral can be used for this too");
	}
}
