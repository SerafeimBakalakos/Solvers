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

	public class MatrixFreeIdentityPreconditionerFactory : IMatrixFreePreconditionerFactory
	{
		public IPreconditioner CreatePreconditioner(IDofScaling dofScaling) => new IdentityPreconditioner();
	}
}
