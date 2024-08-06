namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface INormalizationStrategy
	{
		public void Denormalize(float[] normalizedValues);

		public void Denormalize(double[] normalizedValues);

		public void InitializeAndApply(double[,] samplesAsRows);

		public void InitializeAndApply(float[,] samplesAsRows);

		public void Normalize(float[] sample);

		public void Normalize(double[] sample);
	}
}
