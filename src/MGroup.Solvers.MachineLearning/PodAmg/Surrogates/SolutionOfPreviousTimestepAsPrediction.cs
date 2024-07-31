namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class SolutionOfPreviousTimestepAsPrediction : ISolutionPredictionStrategy
	{
		private int numDofs = -1;
		private SolutionDatabaseDynamic solutionDb;

		public bool MustSaveSolution(int timeStep) => false;

		public double[] Predict(int timeStep, double[] parameters)
		{
			if (timeStep == 0)
			{
				return new double[numDofs];
			}
			else
			{
				return solutionDb.GetCurrentSolution().RawData;
			}
		}

		public void Train(SolutionDatabaseDynamic solutionDb) 
		{
			this.solutionDb = solutionDb;
			numDofs = solutionDb.CountDofs();
		}
	}
}
