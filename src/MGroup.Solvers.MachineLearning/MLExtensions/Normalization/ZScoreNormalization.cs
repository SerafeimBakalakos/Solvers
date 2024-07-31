namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	public class ZScoreNormalization : INormalizationStrategy
	{
		private double[] mean;
		private double[] stdDev;

		public void Denormalize(double[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == mean.Length);
			for (int i = 0; i < mean.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * stdDev[i] + mean[i];
			}
		}

		public void InitializeAndApply(double[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			mean = new double[numFeatures];
			stdDev = new double[numFeatures];

			for (int f = 0; f < numFeatures; f++)
			{
				// Calculate mean
				double currentMean = 0.0;
				for (int s = 0; s < numSamples; s++)
				{
					currentMean += samplesAsRows[s, f];
				}
				currentMean /= numSamples;
				mean[f] = currentMean;

				// Calculate standard deviation
				double currentStdDev = 0.0;
				for (int s = 0; s < numSamples; s++)
				{
					double z = samplesAsRows[s, f] - currentMean;
					currentStdDev += z * z;
				}
				currentStdDev /= (numSamples - 1);
				stdDev[f] = currentStdDev;

				// Apply the normalization to the dataset
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, f] = (samplesAsRows[s, f] - currentMean) / currentStdDev;
				}
			}
		}

		public void Normalize(double[] sample)
		{
			Debug.Assert(sample.Length == mean.Length);
			for (int i = 0; i < sample.Length; i++)
			{
				sample[i] = (sample[i] - mean[i]) / stdDev[i];
			}
		}
	}
}
