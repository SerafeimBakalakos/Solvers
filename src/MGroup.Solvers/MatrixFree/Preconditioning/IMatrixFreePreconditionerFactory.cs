namespace MGroup.Solvers.MatrixFree.Preconditioning
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.Solvers.MatrixFree.Dofs;

	public interface IMatrixFreePreconditionerFactory
	{
		IPreconditioner CreatePreconditioner(IDofScaling dofScaling);
	}
}
