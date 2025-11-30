namespace MGroup.Solvers.DDM.SolversExtensions.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DenseMatrixAssembler_v2 : ISubstructureMatrixAssembler<Matrix>
	{
		public IMatrix BuildSubstructureMatrix(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var substructureMatrix = Matrix.CreateZero(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] substructureDofIndices) = dofOrdering.MapDofsElementToSubstructure(element);
				IMatrix elementMatrix = element.BuildMatrix();
				AddElementToSubstructureMatrix(substructureMatrix, elementMatrix, elementDofIndices, substructureDofIndices);
			}

			return substructureMatrix;
		}

		private static void AddElementToSubstructureMatrix(Matrix substructureMatrix, IReadOnlyMatrix elementMatrix,
			int[] elementIndices, int[] substructureDofIndices)
		{
			Debug.Assert(elementMatrix.NumRows == elementMatrix.NumColumns);
			Debug.Assert(substructureDofIndices.Length == elementIndices.Length);

			int numRelevantRows = elementIndices.Length;
			for (int i = 0; i < numRelevantRows; ++i)
			{
				int elementRow = elementIndices[i];
				int substructureRow = substructureDofIndices[i];
				for (int j = 0; j < numRelevantRows; ++j)
				{
					int elementCol = elementIndices[j];
					int substructureCol = substructureDofIndices[j];

					substructureMatrix[substructureRow, substructureCol] += elementMatrix[elementRow, elementCol];
				}
			}
		}
	}
}
