namespace MGroup.Solvers.Multigrid.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public interface IMultigridSmoother
	{
		void Apply(Vector rhs, Vector solution);

		IMultigridSmoother DeepCopy();

		void UpdateMatrix(IReadOnlyMatrix matrix, bool areDofsModified);
	}
}
