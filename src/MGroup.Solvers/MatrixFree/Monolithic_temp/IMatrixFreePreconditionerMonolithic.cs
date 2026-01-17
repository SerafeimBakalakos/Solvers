namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.MatrixFree.Dofs;

	public interface IMatrixFreePreconditionerMonolithic : IPreconditioner
	{
		void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, IMonolithicDofManager dofManager, IDofScaling dofScaling);
	}
}
