//namespace MGroup.Solvers.DDM.SolversExtensions.DofOrdering
//{
//	using System.Collections.Generic;
//	using System.Linq;

//	using MGroup.LinearAlgebra.Reordering;
//	using MGroup.MSolve.Discretization.Dofs;
//	using MGroup.MSolve.Discretization.Entities;
//	using MGroup.Solvers;
//	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
//	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM;

//	/// <summary>
//	/// Deals with the free (unconstrained) dofs of a subdomain.
//	/// </summary>
//	public class SubdomainFreeDofOrderingGeneral_v2 //: ISubstructureDofOrdering
//	{
//		private readonly DefaultSubstructure_temp substructure;
//		private readonly ActiveDofs allDofs;

//		public SubdomainFreeDofOrderingGeneral_v2(DefaultSubstructure_temp subdstructure, int numFreeDofs, IntDofTable subdomainFreeDofs, 
//			ActiveDofs allDofs)
//		{
//			substructure = subdstructure;
//			NumDofs = numFreeDofs;
//			Dofs = subdomainFreeDofs;
//			this.allDofs = allDofs;
//		}

//		public IntDofTable Dofs { get; }

//		public int NumDofs { get; }

//		public (int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement)
//		{
//			var element = ((DefaultElement_temp)superElement).ElementEntity;

//			var elementNodes = element.DofEnumerator.GetNodesForMatrixAssembly(element);
//			var elementDofs = element.DofEnumerator.GetDofTypesForMatrixAssembly(element);

//			// Count the dof superset (free and constrained) to allocate enough memory and avoid resizing
//			var allElementDofs = 0;
//			for (var i = 0; i < elementNodes.Count; ++i) allElementDofs += elementDofs[i].Count;
//			var elementDofIndices = new List<int>(allElementDofs);
//			var subdomainDofIndices = new List<int>(allElementDofs);

//			var elementDofIdx = 0;
//			for (var nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
//			{
//				for (var dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
//				{
//					var dofID = allDofs.GetIdOfDof(elementDofs[nodeIdx][dofIdx]);
//					var isFree = Dofs.TryGetValue(elementNodes[nodeIdx].ID, dofID, out var subdomainDofIdx);
//					if (isFree)
//					{
//						elementDofIndices.Add(elementDofIdx);
//						subdomainDofIndices.Add(subdomainDofIdx);
//					}

//					++elementDofIdx; // This must be incremented for constrained dofs as well
//				}
//			}

//			return (elementDofIndices.ToArray(), subdomainDofIndices.ToArray());
//		}

//		public void Reorder(IReorderingAlgorithm reorderingAlgorithm) => throw new NotImplementedException("The code in SubstructureDofOrderingGeneral can be used for this too");
//	}
//}
