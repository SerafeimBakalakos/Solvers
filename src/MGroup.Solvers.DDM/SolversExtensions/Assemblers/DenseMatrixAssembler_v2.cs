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

	public class DenseMatrixAssembler_v2 : ISubdomainMatrixAssembler<Matrix>
	{
		public Matrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering)
		{
			int numDofs = dofOrdering.Dofs.NumEntries;
			var subdomainMatrix = Matrix.CreateZero(numDofs, numDofs);

			// Process the stiffness of each element
			foreach (ISuperElement element in subdomain.EnumerateSuperElements())
			{
				// TODO: perhaps that could be done and cached during the dof enumeration to avoid iterating over the dofs twice
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
				IMatrix elementMatrix = element.BuildMatrix();
				AddElementToSubdomainMatrix(subdomainMatrix, elementMatrix, elementDofIndices, subdomainDofIndices);
			}

			return subdomainMatrix;
		}

		private static void AddElementToSubdomainMatrix(Matrix subdomainMatrix, IReadOnlyMatrix elementMatrix,
			int[] elementIndices, int[] subdomainDofIndices)
		{
			Debug.Assert(elementMatrix.NumRows == elementMatrix.NumColumns);
			Debug.Assert(subdomainDofIndices.Length == elementIndices.Length);

			int numRelevantRows = elementIndices.Length;
			for (int i = 0; i < numRelevantRows; ++i)
			{
				int elementRow = elementIndices[i];
				int subdomainRow = subdomainDofIndices[i];
				for (int j = 0; j < numRelevantRows; ++j)
				{
					int elementCol = elementIndices[j];
					int subdomainCol = subdomainDofIndices[j];

					subdomainMatrix[subdomainRow, subdomainCol] += elementMatrix[elementRow, elementCol];
				}
			}
		}
	}
}
