namespace MGroup.Solvers.Multigrid.DirectSolver
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	/// <summary>
	/// Solves the linear systems at the coarsest grid. Optimized for multiple solutions with the same matrix, but different right hand side vectors.
	/// </summary>
	public interface ICoarseSystemSolver : IDisposable
	{
		void Clear();

		void Initialize(IReadOnlyMatrix coarseMatrix);
		
		void Solve(Vector rhs, Vector solution);
	}
}
