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
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering_v2;

	public class DenseMatrixAssembler_v2 : ISubdomainMatrixAssembler_v2<Matrix>
	{
		public Matrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, IMonolithicDofManager dofManager)
		{
			int numDofs = dofManager.NumDomainDofs;
			var subdomainMatrix = Matrix.CreateZero(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateElements())
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
