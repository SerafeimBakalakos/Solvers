namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface ISolutionPredictionStrategy
	{
		public bool MustSaveSolution(int timeStep);

		public double[] Predict(int timeStep, double[] parameters);

		public void Train(SolutionDatabaseDynamic solutionDb);
	}
}
