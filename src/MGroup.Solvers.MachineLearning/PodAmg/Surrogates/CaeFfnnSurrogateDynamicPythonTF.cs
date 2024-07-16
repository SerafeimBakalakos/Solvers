namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MachineLearning.Utilities;

	public class CaeFfnnSurrogateDynamicPythonTF
	{
		private int numDofs;

		public CaeFfnnSurrogateDynamicPythonTF()
		{

		}

		public double[] Predict(int timeStep, double[] parameters)
		{
			return new double[numDofs];
		}

		public void Train(SolutionDatabaseDynamic solutionDb)
		{
			numDofs = solutionDb.CountDofs();

			//double[,] parametersDataset;
			//double[,] solutionsDataset;

			//int numParameters = modelParametersCurrent.Length;
			//var parametersAsArray = new double[numSamples, numParameters];
			//for (int i = 0; i < numSamples; ++i)
			//{
			//	if (PreviousModelParameters[i].Length != numParameters)
			//	{
			//		throw new Exception("The model parameter sets do not all have the same size");
			//	}

			//	for (int j = 0; j < numParameters; ++j)
			//	{
			//		parametersAsArray[i, j] = PreviousModelParameters[i][j];
			//	}
			//}

			//// CAE-FFNN training:  Dimension 0 must be the number of samples.
			//double[,] solutionsAsArray = solutionVectors.Transpose().CopytoArray2D();
			//surrogate.TrainAndEvaluate(parametersAsArray, solutionsAsArray, null);
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
