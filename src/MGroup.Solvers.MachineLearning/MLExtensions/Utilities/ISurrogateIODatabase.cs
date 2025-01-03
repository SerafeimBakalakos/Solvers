namespace MGroup.Solvers.MachineLearning.MLExtensions.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface ISurrogateIODatabase
	{
		void Clear();

		void StoreInputData(float[] inputVector, bool normalized);

		void StoreOutputData(float[] outputVector, bool normalized);

		void StoreTrainingData(float[,] inputVectors, float[,] outputVectors, bool vectorsAsRows, bool normalized);
	}
}
