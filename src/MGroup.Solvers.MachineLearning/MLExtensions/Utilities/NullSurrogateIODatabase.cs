namespace MGroup.Solvers.MachineLearning.MLExtensions.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class NullSurrogateIODatabase : ISurrogateIODatabase
	{
		public void Clear() { } // Do nothing

		public void StoreInputData(float[] inputVector, bool normalized) { } // Do nothing

		public void StoreOutputData(float[] outputVector, bool normalized) { } // Do nothing

		public void StoreTrainingData(float[,] inputVectors, float[,] outputVectors, bool vectorsAsRows, bool normalized) { } // Do nothing
	}
}
