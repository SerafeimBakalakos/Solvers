namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DofOrdering;

	public class GlobalSubstructureVectorAssembler_v2
	{
		private readonly ActiveDofs allDofs;

		public GlobalSubstructureVectorAssembler_v2(ActiveDofs allDofs)
		{
			this.allDofs = allDofs;
		}

		public void AddToSubstructureVector(IEnumerable<INodalModelQuantity<IDofType>> nodalQuantities, Vector substructureVector,
			ISubstructureDofOrdering dofOrdering)
		{
			foreach (INodalModelQuantity<IDofType> nodalQuantity in nodalQuantities)
			{
				int dofIdx = dofOrdering.Dofs[nodalQuantity.Node.ID, allDofs.GetIdOfDof(nodalQuantity.DOF)];
				substructureVector[dofIdx] += nodalQuantity.Amount;
			}
		}

		//public IVector BuildSubstructureVector(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		//{
		//	int numDofs = dofOrdering.Dofs.NumEntries;
		//	var substructureVector = Vector.CreateZero(numDofs);

		//	// Process the stiffness of each element
		//	foreach (ISuperElement element in substructure.EnumerateSuperElements())
		//	{
		//		// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
		//		(int[] elementDofIndices, int[] substructureDofIndices) = dofOrdering.MapDofsElementToSubstructure(element);
		//		//IReadOnlyDictionary<int, int> elementToGlobalDofs = dofOrdering.MapFreeDofsElementToSubdomain(element);
		//		IVector elementVector = element.BuildRhsVector();
		//		substructureVector.AddIntoThisNonContiguouslyFrom(substructureDofIndices, elementVector, elementDofIndices);
		//	}

		//	return substructureVector;
		//}
	}
}
