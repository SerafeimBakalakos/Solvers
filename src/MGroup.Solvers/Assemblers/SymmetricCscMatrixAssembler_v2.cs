namespace MGroup.Solvers.Assemblers
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
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DiscretizationExtensions;

	public class SymmetricCscMatrixAssembler_v2 : ISubdomainMatrixAssembler_v2<SymmetricCscMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public SymmetricCscMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public SymmetricCscMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering)
		{
			int numDofs = dofOrdering.DomainDofs.NumEntries;
			var subdomainMatrix = DokSymmetric.CreateEmpty(numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateElements())
			{
				int[] elementToDomainDofs = dofOrdering.MapDofsElementToDomain(element);
				int[] temp = Enumerable.Range(0, elementToDomainDofs.Length).ToArray();
				IMatrix elementMatrix = element.BuildMatrix();
				subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, temp, elementToDomainDofs);
			}

			(double[] values, int[] rowIndices, int[] colOffsets) = subdomainMatrix.BuildSymmetricCscArrays(sortColsOfEachRow);
			return SymmetricCscMatrix.CreateFromArrays(numDofs, values, rowIndices, colOffsets, false);
		}

		public void HandleDofOrderingWasModified()
		{
		}
	}
}
