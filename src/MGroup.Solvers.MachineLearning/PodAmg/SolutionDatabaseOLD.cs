namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution.LinearSystem;

	public class SolutionDatabaseOLD
	{
		private readonly Dictionary<int, Dictionary<int, IGlobalVector>> savedSolutions;

		public SolutionDatabaseOLD()
		{
			savedSolutions = new Dictionary<int, Dictionary<int, IGlobalVector>>();
		}

		public int NumTimeSteps => savedSolutions.Values.First().Count;

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
