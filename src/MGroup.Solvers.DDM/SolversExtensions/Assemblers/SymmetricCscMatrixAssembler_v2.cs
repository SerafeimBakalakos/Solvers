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
	using MGroup.LinearAlgebra.Matrices.Builders;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class SymmetricCscMatrixAssembler_v2 : ISubstructureMatrixAssembler<SymmetricCscMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public SymmetricCscMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public IMatrix BuildSubstructureMatrix(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var substructureMatrix = DokSymmetric.CreateEmpty(numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in substructure.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] substructureDofIndices) = dofOrdering.MapDofsElementToSubstructure(element);
				IMatrix elementMatrix = element.BuildMatrix();
				substructureMatrix.AddSubmatrixSymmetric(elementMatrix, elementDofIndices, substructureDofIndices);
			}

			(double[] values, int[] rowIndices, int[] colOffsets) = substructureMatrix.BuildSymmetricCscArrays(sortColsOfEachRow);
			return SymmetricCscMatrix.CreateFromArrays(numDofs, values, rowIndices, colOffsets, false);
		}
	}
}
