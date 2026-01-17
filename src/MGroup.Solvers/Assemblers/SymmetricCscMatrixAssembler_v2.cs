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
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.Discretization;

	public class SymmetricCscMatrixAssembler_v2 : IDomainMatrixAssembler_v2<SymmetricCscMatrix>
	{
		private readonly bool sortColsOfEachRow;

		public SymmetricCscMatrixAssembler_v2(bool sortColsOfEachRow = true)
		{
			this.sortColsOfEachRow = sortColsOfEachRow;
		}

		public SymmetricCscMatrix BuildDomainMatrix(IDomain domain, IMonolithicDofManager dofManager)
		{
			int numDofs = dofManager.NumDomainDofs;
			var subdomainMatrix = DokSymmetric_v2.CreateEmpty(numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				int[] elementToDomainDofs = dofManager.MapDofsElementToDomain(element);
				IMatrix elementMatrix = element.BuildMatrix();
				subdomainMatrix.AddSubmatrixSymmetric(elementMatrix, elementToDomainDofs);
			}

			(double[] values, int[] rowIndices, int[] colOffsets) = subdomainMatrix.BuildSymmetricCscArrays(sortColsOfEachRow);
			return SymmetricCscMatrix.CreateFromArrays(numDofs, values, rowIndices, colOffsets, false);
		}

		public void HandleDofOrderingWasModified()
		{
		}
	}
}
