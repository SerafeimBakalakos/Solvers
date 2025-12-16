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
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class SymmetricCscMatrixAssembler_v2 : ISubdomainMatrixAssembler<SymmetricCscMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public SymmetricCscMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public IMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var subdomainMatrix = DokSymmetric.CreateEmpty(numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
				IMatrix elementMatrix = element.BuildMatrix();
				subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, elementDofIndices, subdomainDofIndices);
			}

			(double[] values, int[] rowIndices, int[] colOffsets) = subdomainMatrix.BuildSymmetricCscArrays(sortColsOfEachRow);
			return SymmetricCscMatrix.CreateFromArrays(numDofs, values, rowIndices, colOffsets, false);
		}
	}
}
