namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;

	public interface ITempSolver : ISolver
	{
		public IAlgebraicModel Model { get; }

		public SolutionDatabaseOLD SavedSolutions { get; }

		public void OnModelParameterUpdate(int parameterSet);
	}
}
