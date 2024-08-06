namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	public class MinMaxNormalization : INormalizationStrategy
	{
		private double[] min64;
		private double[] maxMinusMin64;

		private float[] min32;
		private float[] maxMinusMin32;


		public void Denormalize(double[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == min64.Length);
			for (int i = 0; i < min64.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * maxMinusMin64[i] + min64[i];
			}
		}

		public void Denormalize(float[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == min32.Length);
			for (int i = 0; i < min32.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * maxMinusMin32[i] + min32[i];
			}
		}

		public void InitializeAndApply(double[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			min64 = new double[numFeatures];
			maxMinusMin64 = new double[numFeatures];

			for (int i = 0; i < numFeatures; i++)
			{
				// Locate min, max
				double currentMin = double.MaxValue;
				double currentMax = double.MinValue;
				for (int s = 0; s < numSamples; s++)
				{
					double x = samplesAsRows[s, i];
					if (x < currentMin)
					{
						currentMin = x;
					}
					if (x > currentMax)
					{
						currentMax = x;
					}
				}

				// Save them
				double xmaxMinusXmin = currentMax - currentMin;
				min64[i] = currentMin;
				maxMinusMin64[i] = xmaxMinusXmin;

				// Apply the normalization to the dataset
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, i] = (samplesAsRows[s, i] - currentMin) / xmaxMinusXmin;
				}
			}
		}

		public void InitializeAndApply(float[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			min32 = new float[numFeatures];
			maxMinusMin32 = new float[numFeatures];

			for (int f = 0; f < numFeatures; f++)
			{
				// Locate min, max
				float currentMin = float.MaxValue;
				float currentMax = float.MinValue;
				for (int s = 0; s < numSamples; s++)
				{
					float x = samplesAsRows[s, f];
					if (x < currentMin)
					{
						currentMin = x;
					}
					if (x > currentMax)
					{
						currentMax = x;
					}
				}

				// Save them
				float xmaxMinusXmin = currentMax - currentMin;
				min32[f] = currentMin;
				maxMinusMin32[f] = xmaxMinusXmin;

				// Apply the normalization to the dataset
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, f] = (samplesAsRows[s, f] - currentMin) / xmaxMinusXmin;
				}
			}
		}

		public void Normalize(double[] sample)
		{
			Debug.Assert(sample.Length == min64.Length);
			for (int i = 0; i < min64.Length; i++)
			{
				sample[i] = (sample[i] - min64[i]) / maxMinusMin64[i];
			}
		}

		public void Normalize(float[] sample)
		{
			Debug.Assert(sample.Length == min32.Length);
			for (int i = 0; i < min32.Length; i++)
			{
				sample[i] = (sample[i] - min32[i]) / maxMinusMin32[i];
			}
		}
	}
}
