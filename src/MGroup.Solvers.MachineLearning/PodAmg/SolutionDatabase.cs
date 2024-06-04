namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution.LinearSystem;

	public class SolutionDatabase
	{
		private readonly Dictionary<int, Dictionary<int, IGlobalVector>> savedSolutions;

		public SolutionDatabase()
		{
			savedSolutions = new Dictionary<int, Dictionary<int, IGlobalVector>>();
		}

		public IGlobalVector GetSolution(int parameterSet, int timeStep)
		{
			return savedSolutions[parameterSet][timeStep];
		}

		public void SaveSolution(int parameterSet, int timeStep, IGlobalVector solution)
		{
			Dictionary<int, IGlobalVector> solutionsOfRealization;
			if (!savedSolutions.TryGetValue(parameterSet, out solutionsOfRealization))
			{
				solutionsOfRealization = new Dictionary<int, IGlobalVector>();
				savedSolutions[parameterSet] = solutionsOfRealization;
			}
			solutionsOfRealization[timeStep] = solution.Copy();
		}
	}
}
