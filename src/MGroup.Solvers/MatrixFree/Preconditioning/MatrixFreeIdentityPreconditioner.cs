namespace MGroup.Solvers.MatrixFree.Preconditioning
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.MatrixFree.Dofs;

	public class MatrixFreeIdentityPreconditioner : IMatrixFreePreconditioner
	{
		public IPreconditioner CopyWithInitialSettings() => new MatrixFreeIdentityPreconditioner();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector) => lhsVector.CopyFrom(rhsVector);
		
		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering, IDofScaling dofScaling) => throw new NotImplementedException();

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) {}
	}
}
