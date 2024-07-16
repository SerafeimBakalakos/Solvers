namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MachineLearning.Utilities;

	public class CaeFfnnSurrogateDynamicPythonTF
	{
		public CaeFfnnSurrogateDynamicPythonTF()
		{

		}

		public double[] Predict(double[] parameters)
		{
			throw new NotImplementedException();
		}

		public void Train(SolutionDatabaseDynamic solutionDb)
		{
			double[,] parametersDataset;
			double[,] solutionsDataset;
			

		}

		public class Builder
		{
			public Builder()
			{
			}

			public CaeFfnnSurrogateDynamicPythonTF BuildSurrogate()
			{
				return new CaeFfnnSurrogateDynamicPythonTF();
			}
		}
	}
}
