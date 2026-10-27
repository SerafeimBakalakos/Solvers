namespace MGroup.Solvers.Multigrid.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Multigrid.Logging;

	public interface IMultigridAlgorithm
	{
		public MultigridSolverLogger Logger { get; }

		public string Name { get; }

		/// <summary>
		/// Prepare data that do not change based on the linear system.
		/// </summary>
		void Initialize();

		void PrepareForNewLinearSystemMatrix(CsrMatrix systemMatrix);

		void RunSingleCycle(Vector rhsVector, Vector solution);

		void SolveLinearSystem(Vector rhsVector, Vector solution);
	}
}
