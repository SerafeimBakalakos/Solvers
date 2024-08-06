namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	public class ZScoreNormalization : INormalizationStrategy
	{
		private double[] mean64;
		private double[] stdDev64;

		private float[] mean32;
		private float[] stdDev32;

		public void Denormalize(double[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == mean64.Length);
			for (int i = 0; i < mean64.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * stdDev64[i] + mean64[i];
			}
		}

		public void Denormalize(float[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == mean32.Length);
			for (int i = 0; i < mean32.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * stdDev32[i] + mean32[i];
			}
		}

		public void InitializeAndApply(double[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			mean64 = new double[numFeatures];
			stdDev64 = new double[numFeatures];

			for (int f = 0; f < numFeatures; f++)
			{
				// Calculate mean
				double currentMean = 0.0;
				for (int s = 0; s < numSamples; s++)
				{
					currentMean += samplesAsRows[s, f];
				}
				currentMean /= numSamples;
				mean64[f] = currentMean;

				// Calculate standard deviation
				double currentStdDev = 0.0;
				for (int s = 0; s < numSamples; s++)
				{
					double z = samplesAsRows[s, f] - currentMean;
					currentStdDev += z * z;
				}
				currentStdDev = Math.Sqrt(currentStdDev / (numSamples - 1));
				stdDev64[f] = currentStdDev;

				// Apply the normalization to the dataset
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, f] = (samplesAsRows[s, f] - currentMean) / currentStdDev;
				}
			}
		}

		public void InitializeAndApply(float[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			mean32 = new float[numFeatures];
			stdDev32 = new float[numFeatures];

			for (int i = 0; i < numFeatures; i++)
			{
				// Calculate mean
				double currentMean = 0.0; // temporatily use double-precision to be more accurate 
				for (int s = 0; s < numSamples; s++)
				{
					currentMean += samplesAsRows[s, i];
				}
				currentMean /= numSamples;
				mean32[i] = (float)currentMean;

				// Calculate standard deviation
				double currentStdDev = 0.0; // temporatily use double-precision to be more accurate 
				for (int s = 0; s < numSamples; s++)
				{
					double z = samplesAsRows[s, i] - currentMean;
					currentStdDev += z * z;
				}
				currentStdDev = Math.Sqrt(currentStdDev / (numSamples - 1));
				stdDev32[i] = (float)currentStdDev;

				// Apply the normalization to the dataset
				float currentMean32 = mean32[i];
				float currentStdDev32 = stdDev32[i];
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, i] = (samplesAsRows[s, i] - currentMean32) / currentStdDev32;
				}
			}
		}

		public void Normalize(double[] sample)
		{
			Debug.Assert(sample.Length == mean64.Length);
			for (int i = 0; i < sample.Length; i++)
			{
				sample[i] = (sample[i] - mean64[i]) / stdDev64[i];
			}
		}

		public void Normalize(float[] sample)
		{
			Debug.Assert(sample.Length == mean32.Length);
			for (int i = 0; i < sample.Length; i++)
			{
				sample[i] = (sample[i] - mean32[i]) / stdDev32[i];
			}
		}
	}
}
