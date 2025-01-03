namespace MGroup.Solvers.MachineLearning.MLExtensions.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class SurrogateIODatabase : ISurrogateIODatabase
	{
		public SurrogateIODatabase() { }

		public List<float[]> InputVectors { get; } = new List<float[]>();

		public List<float[]> InputVectorsNormalized { get; } = new List<float[]>();

		public List<float[]> OutputVectors { get; } = new List<float[]>();

		public List<float[]> OutputVectorsNormalized { get; } = new List<float[]>();

		public void Clear()
		{
			InputVectors.Clear();
			InputVectorsNormalized.Clear();
			OutputVectors.Clear();
			OutputVectorsNormalized.Clear();
		}


		public void StoreInputData(float[] inputVector, bool normalized)
		{
			if (normalized)
			{
				if (InputVectorsNormalized.Count > OutputVectorsNormalized.Count)
				{
					throw new InvalidOperationException("An input vector, that does not correspond to an output one, has" +
						" already been stored. Before adding any more input vectors, an output vector must be stored.");
				}
				InputVectorsNormalized.Add(inputVector);
			}
			else
			{
				if (InputVectors.Count > OutputVectors.Count)
				{
					throw new InvalidOperationException("An input vector, that does not correspond to an output one, has" +
						" already been stored. Before adding any more input vectors, an output vector must be stored.");
				}
				InputVectors.Add(inputVector);
			}
		}

		public void StoreOutputData(float[] outputVector, bool normalized)
		{
			if (normalized)
			{
				if (InputVectorsNormalized.Count <= OutputVectorsNormalized.Count)
				{
					throw new InvalidOperationException(
						"Before storing an output vector, its corresponding input vector must be stored.");
				}
				OutputVectorsNormalized.Add(outputVector);
			}
			else
			{
				if (InputVectors.Count <= OutputVectors.Count)
				{
					throw new InvalidOperationException(
						"Before storing an output vector, its corresponding input vector must be stored.");
				}
				OutputVectors.Add(outputVector);
			}
		}

		public void StoreTrainingData(float[,] inputVectors, float[,] outputVectors, bool vectorsAsRows, bool normalized)
		{
			if (vectorsAsRows)
			{
				int numSamples = inputVectors.GetLength(0);
				if (outputVectors.GetLength(0) != numSamples)
				{
					throw new ArgumentException(
						$"There are {numSamples} input vectors, but {outputVectors.GetLength(0)} output vectors");
				}

				for (int i = 0; i < numSamples; i++)
				{
					float[] inputVector = ExtractRow(inputVectors, i);
					float[] outputVector = ExtractRow(outputVectors, i);
					RegisterIOPair(inputVector, outputVector, normalized);
				}
			}
			else
			{
				int numSamples = inputVectors.GetLength(1);
				if (outputVectors.GetLength(1) != numSamples)
				{
					throw new ArgumentException(
						$"There are {numSamples} input vectors, but {outputVectors.GetLength(1)} output vectors");
				}

				for (int j = 0; j < numSamples; j++)
				{
					float[] inputVector = ExtractColumn(inputVectors, j);
					float[] outputVector = ExtractColumn(outputVectors, j);
					RegisterIOPair(inputVector, outputVector, normalized);
				}
			}
			
		}

		private void RegisterIOPair(float[] inputVector, float[] outputVector, bool normalized)
		{
			//TODO: Perhaps check that dimensions match
			if (normalized)
			{
				InputVectorsNormalized.Add(inputVector);
				OutputVectorsNormalized.Add(outputVector);
			}
			else
			{
				InputVectors.Add(inputVector);
				OutputVectors.Add(outputVector);
			}
		}

		private static float[] ExtractColumn(float[,] array2D, int columnIdx)
		{
			int colLength = array2D.GetLength(0);
			var colVector = new float[colLength];
			for (int i = 0; i < colLength; i++)
			{
				colVector[i] = array2D[i, columnIdx];
			}

			return colVector;
		}

		private static float[] ExtractRow(float[,] array2D, int rowIdx)
		{
			int rowLength = array2D.GetLength(1);
			var rowVector = new float[rowLength];
			for (int j = 0; j < rowLength; j++)
			{
				rowVector[j] = array2D[rowIdx, j];
			}

			return rowVector;
		}
	}
}
