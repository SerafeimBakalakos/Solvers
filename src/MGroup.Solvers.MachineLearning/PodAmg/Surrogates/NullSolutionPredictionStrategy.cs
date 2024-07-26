namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class NullSolutionPredictionStrategy : ISolutionPredictionStrategy
	{
		private int numDofs = -1;

		public double[] Predict(int timeStep, double[] parameters)
		{
			return new double[numDofs];
		}

		public void Train(SolutionDatabaseDynamic solutionDb)
		{
			numDofs = solutionDb.CountDofs();
		}
	}
}
