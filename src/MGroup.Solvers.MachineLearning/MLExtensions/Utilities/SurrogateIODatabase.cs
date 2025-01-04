namespace MGroup.Solvers.MachineLearning.MLExtensions.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.IO;
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

		public (float[,] f, float[,] x, float[,] y) ExtractSurfaceGraph(bool normalized)
		{
			CheckExistingData(normalized);

			List<float[]> inputs;
			List<float[]> outputs;
			if (normalized)
			{
				inputs = InputVectorsNormalized;
				outputs = OutputVectorsNormalized;
			}
			else
			{
				inputs = InputVectors;
				outputs = OutputVectors;
			}

			if (inputs[0].Length != 2)
			{
				throw new InvalidOperationException($"For a surface graph the model parameters must be 2," +
					$" but there are {inputs[0].Length} model parameters.");
			}
			if (outputs[0].Length != 1)
			{
				throw new NotImplementedException();
			}

			// Collect the data in temporary structures
			int numSamples = inputs.Count;
			var xSet = new SortedSet<float>();
			var ySet = new SortedSet<float>();
			for (int i = 0; i < numSamples; i++)
			{
				xSet.Add(inputs[i][0]);
				ySet.Add(inputs[i][1]);
			}

			var xIndices = new Dictionary<float, int>();
			int idx = 0;
			foreach (float x in xSet)
			{
				xIndices[x] = idx;
				idx++; 
			}

			var yIndices = new Dictionary<float, int>();
			idx = 0;
			foreach (float y in ySet)
			{
				yIndices[y] = idx;
				idx++;
			}

			int numX = xSet.Count;
			int numY = ySet.Count;
			var xResult = new float[numX, numY];
			var yResult = new float[numX, numY];
			var fResult = new float[numX, numY];
			for (int i = 0; i < numSamples; i++)
			{
				float x = inputs[i][0];
				float y = inputs[i][1];
				float f = outputs[i][0];

				int xIdx = xIndices[x];
				int yIdx = yIndices[y];
				xResult[xIdx, yIdx] = x;
				yResult[xIdx, yIdx] = y;
				fResult[xIdx, yIdx] = f;
			}

			return (fResult, xResult, yResult);
		}

		public void StoreInputData(float[] inputVector, bool normalized)
		{
			if (normalized)
			{
				//if (InputVectorsNormalized.Count > OutputVectorsNormalized.Count)
				//{
				//	throw new InvalidOperationException("An input vector, that does not correspond to an output one, has" +
				//		" already been stored. Before adding any more input vectors, an output vector must be stored.");
				//}
				InputVectorsNormalized.Add(inputVector);
			}
			else
			{
				//if (InputVectors.Count > OutputVectors.Count)
				//{
				//	throw new InvalidOperationException("An input vector, that does not correspond to an output one, has" +
				//		" already been stored. Before adding any more input vectors, an output vector must be stored.");
				//}
				InputVectors.Add(inputVector);
			}
		}

		public void StoreOutputData(float[] outputVector, bool normalized)
		{
			if (normalized)
			{
				//if (InputVectorsNormalized.Count <= OutputVectorsNormalized.Count)
				//{
				//	throw new InvalidOperationException(
				//		"Before storing an output vector, its corresponding input vector must be stored.");
				//}
				OutputVectorsNormalized.Add(outputVector);
			}
			else
			{
				//if (InputVectors.Count <= OutputVectors.Count)
				//{
				//	throw new InvalidOperationException(
				//		"Before storing an output vector, its corresponding input vector must be stored.");
				//}
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

		public void WriteSurfaceGraphForMatlab(bool normalized, string directory)
		{
			(float[,] f, float[,] x, float[,] y) = ExtractSurfaceGraph(normalized);
			string suffix = normalized ? "_normalized" : "_unnormalized";
			WriteMatlabArray2D(f, Path.Combine(directory, $"f{suffix}.txt"));
			WriteMatlabArray2D(x, Path.Combine(directory, $"x{suffix}.txt"));
			WriteMatlabArray2D(y, Path.Combine(directory, $"y{suffix}.txt"));
		}

		private void CheckExistingData(bool normalized)
		{
			if (normalized)
			{
				if (InputVectorsNormalized.Count == 0)
				{
					throw new InvalidOperationException("There are no data stored yet.");
				}

				if (OutputVectorsNormalized.Count != InputVectorsNormalized.Count)
				{
					throw new InvalidOperationException($"There are {InputVectorsNormalized.Count} input vectors," +
						$" but {OutputVectorsNormalized.Count} output vectors stored.");
				}
			}
			else
			{
				if (InputVectors.Count == 0)
				{
					throw new InvalidOperationException("There are no data stored yet.");
				}

				if (OutputVectors.Count != InputVectors.Count)
				{
					throw new InvalidOperationException($"There are {InputVectors.Count} input vectors," +
						$" but {OutputVectors.Count} output vectors stored.");
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

		private static void WriteMatlabArray2D(float[,] array2D, string path) //TODO: Have linear algebra support for this
		{
			string colDelimiter = " ";
			int numRows = array2D.GetLength(0);
			int numCols = array2D.GetLength(1);

			using (var writer = new StreamWriter(path))
			{
				// First row
				writer.Write(array2D[0, 0]);
				for (int j = 1; j < numCols; j++)
				{
					writer.Write(colDelimiter);
					writer.Write(array2D[0, j]);
				}

				// Subsequent rows
				for (int i = 1; i < numRows; i++)
				{
					writer.WriteLine();
					writer.Write(array2D[i, 0]);
					for (int j = 1; j < numCols; j++)
					{
						writer.Write(colDelimiter);
						writer.Write(array2D[i, j]);
					}
				}
			}
		}
	}
}
