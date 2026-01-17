namespace MGroup.Solvers.MatrixFree.Monolithic
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
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.Preconditioning;

	public class MatrixFreeJacobiPreconditionerMonolithic : IMatrixFreePreconditionerMonolithic
	{
		private IMonolithicDofManager dofManager;
		private IReadOnlyCollection<ISuperElement> elements;
		private DiagonalMatrix inverseDiagonalMatrix;

		public IPreconditioner CopyWithInitialSettings() => new MatrixFreeJacobiPreconditionerMonolithic();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			inverseDiagonalMatrix.MultiplyIntoResult(rhsVector, lhsVector);
		}

		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, IMonolithicDofManager dofManager, IDofScaling dofScaling)
		{
			if (systemMatrix is ElementWiseMatrixMonolithic partitionedMatrix)
			{
				this.dofManager = dofManager;
				this.elements = elements;
				inverseDiagonalMatrix = DiagonalMatrix.CreateZero(dofManager.NumDomainDofs);
				foreach (ISuperElement element in elements)
				{
					int[] elementToDomainDofs = dofManager.MapDofsElementToDomain(element);
					IReadOnlyMatrix elementMatrix = partitionedMatrix.ElementMatrices[element.ID];
					inverseDiagonalMatrix.AddSubmatrix(elementToDomainDofs, elementMatrix.GetDiagonalAsArray());
				}
				inverseDiagonalMatrix.Invert();
			}
			else
			{
				throw new NonMatchingFormatException($"Can only operate on {nameof(ElementWiseMatrixMonolithic)}");
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) => throw new NotImplementedException();
	}
}
