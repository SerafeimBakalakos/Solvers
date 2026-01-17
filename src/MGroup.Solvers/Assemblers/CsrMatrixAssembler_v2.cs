namespace MGroup.Solvers.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public class CsrMatrixAssembler_v2 : ISubdomainMatrixAssembler_v2<CsrMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public CsrMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public CsrMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, IMonolithicDofManager dofManager)
		{
			int numDofs = dofManager.NumDomainDofs;
			var subdomainMatrix = DokRowMajor_v2.CreateEmpty(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateElements())
			{
				int[] elementToDomainDofs = dofManager.MapDofsElementToDomain(element);
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
