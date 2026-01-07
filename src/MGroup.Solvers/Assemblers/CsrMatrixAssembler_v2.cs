namespace MGroup.Solvers.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public class CsrMatrixAssembler_v2 : ISubdomainMatrixAssembler_v2<CsrMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public CsrMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public CsrMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering)
		{
			int numDofs = dofOrdering.DomainDofs.NumEntries;
			var subdomainMatrix = DokRowMajor_v2.CreateEmpty(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateElements())
			{
				int[] elementToDomainDofs = dofOrdering.MapDofsElementToDomain(element);
				IMatrix elementMatrix = element.BuildMatrix();
				subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, elementToDomainDofs);
			}

			(double[] values, int[] colIndices, int[] rowOffsets) = subdomainMatrix.BuildCsrArrays(sortColsOfEachRow);
			return CsrMatrix.CreateFromArrays(numDofs, numDofs, values, colIndices, rowOffsets, false);
		}

		public void HandleDofOrderingWasModified()
		{
		}
	}
}
