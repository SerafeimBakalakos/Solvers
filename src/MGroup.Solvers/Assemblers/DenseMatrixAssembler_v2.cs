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
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.Discretization;

	public class DenseMatrixAssembler_v2 : IDomainMatrixAssembler_v2<Matrix>
	{
		public Matrix BuildDomainMatrix(IDomain domain, IMonolithicDofManager dofManager)
		{
			int numDofs = dofManager.NumDomainDofs;
			var subdomainMatrix = Matrix.CreateZero(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in domain.EnumerateElements())
			{
				int[] elementToDomainDofs = dofManager.MapDofsElementToDomain(element);
				IMatrix elementMatrix = element.BuildMatrix();
				AddElementToSubdomainMatrix(subdomainMatrix, elementMatrix, elementToDomainDofs);
			}

			return subdomainMatrix;
		}

		public void HandleDofOrderingWasModified()
		{
		}

		private static void AddElementToSubdomainMatrix(Matrix subdomainMatrix, IReadOnlyMatrix elementMatrix, int[] elementToSubdomainDofs)
		{
			int order = elementMatrix.NumColumns;
			Debug.Assert(elementMatrix.NumRows == order);
			Debug.Assert(elementToSubdomainDofs.Length == order);

			for (int i = 0; i < order; ++i)
			{
				int subdomainRow = elementToSubdomainDofs[i];
				for (int j = 0; j < order; ++j)
				{
					int subdomainCol = elementToSubdomainDofs[j];
					subdomainMatrix[subdomainRow, subdomainCol] += elementMatrix[i, j];
				}
			}
		}
	}
}
