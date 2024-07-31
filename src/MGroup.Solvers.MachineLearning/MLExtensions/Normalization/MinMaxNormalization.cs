namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	public class MinMaxNormalization : INormalizationStrategy
	{
		private double[] min;
		private double[] maxMinusMin;

		public void Denormalize(double[] normalizedValues)
		{
			Debug.Assert(normalizedValues.Length == min.Length);
			for (int i = 0; i < min.Length; i++)
			{
				normalizedValues[i] = normalizedValues[i] * maxMinusMin[i] + min[i];
			}
		}

		public void InitializeAndApply(double[,] samplesAsRows)
		{
			int numSamples = samplesAsRows.GetLength(0);
			int numFeatures = samplesAsRows.GetLength(1);
			min = new double[numFeatures];
			maxMinusMin = new double[numFeatures];

			for (int f = 0; f < numFeatures; f++)
			{
				// Locate min, max
				double currentMin = double.MaxValue;
				double currentMax = double.MinValue;
				for (int s = 0; s < numSamples; s++)
				{
					double x = samplesAsRows[s, f];
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
				min[f] = currentMin;
				maxMinusMin[f] = xmaxMinusXmin;

				// Apply the normalization to the dataset
				for (int s = 0; s < numSamples; s++)
				{
					samplesAsRows[s, f] = (samplesAsRows[s, f] - currentMin) / xmaxMinusXmin;
				}
			}
		}

		public void Normalize(double[] sample)
		{
			Debug.Assert(sample.Length == min.Length);
			for (int i = 0; i < min.Length; i++)
			{
				sample[i] = (sample[i] - min[i]) / maxMinusMin[i];
			}
		}
	}
}
