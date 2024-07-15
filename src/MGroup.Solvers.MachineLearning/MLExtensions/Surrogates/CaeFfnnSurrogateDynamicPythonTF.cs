namespace MGroup.Solvers.MachineLearning.MLExtensions.Surrogates
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

		public void Train(double[,] parametersDataset, double[,] solutionsDataset)
		{
			throw new NotImplementedException();
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
