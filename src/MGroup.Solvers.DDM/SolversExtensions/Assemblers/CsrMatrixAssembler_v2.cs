namespace MGroup.Solvers.DDM.SolversExtensions.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Matrices.Builders;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class CsrMatrixAssembler_v2 : ISubstructureMatrixAssembler<Matrix>
	{
		private readonly bool sortColsOfEachRow;

		public CsrMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public IMatrix BuildSubstructureMatrix(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var substructureMatrix = DokRowMajor.CreateEmpty(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] substructureDofIndices) = dofOrdering.MapDofsElementToSubstructure(element);
				IMatrix elementMatrix = element.BuildMatrix();
				substructureMatrix.AddSubmatrixSymmetric(elementMatrix, elementDofIndices, substructureDofIndices);
			}

			(double[] values, int[] colIndices, int[] rowOffsets) = substructureMatrix.BuildCsrArrays(sortColsOfEachRow);
			return CsrMatrix.CreateFromArrays(numDofs, numDofs, values, colIndices, rowOffsets, false);
		}
	}
}
