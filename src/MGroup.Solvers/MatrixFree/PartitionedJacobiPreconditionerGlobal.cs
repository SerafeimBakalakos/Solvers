namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions;

	public class PartitionedJacobiPreconditionerGlobal : IPreconditioner
	{
		private ISubdomainDofOrdering_v2 dofOrdering;
		private IReadOnlyCollection<ISuperElement> elements;
		private DiagonalMatrix inverseDiagonalMatrix;

		public PartitionedJacobiPreconditionerGlobal()
		{
		}

		public IPreconditioner CopyWithInitialSettings() => throw new NotImplementedException();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			inverseDiagonalMatrix.MultiplyIntoResult(rhsVector, lhsVector);
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			if (matrix is PartitionedMatrixGlobal partitionedMatrix)
			{
				this.dofOrdering = partitionedMatrix.DofOrdering;
				this.elements = partitionedMatrix.Elements;
				inverseDiagonalMatrix = DiagonalMatrix.CreateZero(dofOrdering.NumDofs);
				foreach (ISuperElement element in elements)
				{
					(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
					IReadOnlyMatrix elementMatrix = partitionedMatrix.ElementMatrices[element.ID];
					inverseDiagonalMatrix.AddSubmatrix(subdomainDofIndices, elementMatrix.GetDiagonalAsArray());
				}
				inverseDiagonalMatrix.Invert();
			}
			else
			{
				throw new NonMatchingFormatException($"Can only operate on {nameof(PartitionedMatrixGlobal)}");
			}
		}
	}
}
