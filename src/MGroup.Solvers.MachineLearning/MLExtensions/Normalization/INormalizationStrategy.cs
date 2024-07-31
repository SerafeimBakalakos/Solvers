namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface INormalizationStrategy
	{
		public void Denormalize(double[] normalizedValues);

		public void InitializeAndApply(double[,] samplesAsRows);

		public void Normalize(double[] sample);
	}
}
