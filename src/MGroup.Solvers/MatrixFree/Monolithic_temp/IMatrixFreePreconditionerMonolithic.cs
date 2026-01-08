namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.MatrixFree.Dofs;

	public interface IMatrixFreePreconditionerMonolithic : IPreconditioner
	{
		void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering, IDofScaling dofScaling);
	}
}
