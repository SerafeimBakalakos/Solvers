namespace MGroup.Solvers.Multigrid.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public interface IMultigridAlgorithm
	{
		/// <summary>
		/// Prepare data that do not change based on the linear system.
		/// </summary>
		void Initialize();

		void PrepareForNewLinearSystemMatrix(CsrMatrix systemMatrix);

		void RunSingleCycle(Vector rhsVector, Vector solution);

		void SolveLinearSystem(Vector rhsVector, Vector solution);
	}
}
