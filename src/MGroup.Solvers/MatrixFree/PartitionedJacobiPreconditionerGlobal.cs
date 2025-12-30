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

	public class PartitionedJacobiPreconditionerGlobal : IMatrixFreePreconditioner
	{
		private ISubdomainDofOrdering_v2 dofOrdering;
		private IReadOnlyCollection<ISuperElement> elements;
		private DiagonalMatrix inverseDiagonalMatrix;

		public IPreconditioner CopyWithInitialSettings() => new PartitionedJacobiPreconditionerGlobal();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			inverseDiagonalMatrix.MultiplyIntoResult(rhsVector, lhsVector);
		}

		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering, IDofScaling dofScaling)
		{
			if (systemMatrix is PartitionedMatrixGlobal partitionedMatrix)
			{
				this.dofOrdering = dofOrdering;
				this.elements = elements;
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

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) => throw new NotImplementedException();
	}
}
