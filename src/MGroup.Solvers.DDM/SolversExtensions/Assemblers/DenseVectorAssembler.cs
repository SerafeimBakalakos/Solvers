namespace MGroup.Solvers.DDM.SolversExtensions.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DenseVectorAssembler
	{
		public IVector BuildSubstructureVector(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var substructureVector = Vector.CreateZero(numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] substructureDofIndices) = dofOrdering.MapDofsElementToSubstructure(element);
				//IReadOnlyDictionary<int, int> elementToGlobalDofs = dofOrdering.MapFreeDofsElementToSubdomain(element);
				IVector elementVector = element.BuildRhsVector();
				substructureVector.AddIntoThisNonContiguouslyFrom(substructureDofIndices, elementVector, elementDofIndices);
			}

			return substructureVector;
		}
	}
}
