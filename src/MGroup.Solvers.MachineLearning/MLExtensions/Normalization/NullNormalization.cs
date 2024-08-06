namespace MGroup.Solvers.MachineLearning.MLExtensions.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class NullNormalization : INormalizationStrategy
	{
		public void Denormalize(double[] normalizedValues) { } // Do nothing

		public void Denormalize(float[] normalizedValues) { } // Do nothing

		public void InitializeAndApply(double[,] samplesAsRows) { } // Do nothing

		public void InitializeAndApply(float[,] samplesAsRows) { } // Do nothing

		public void Normalize(double[] sample) { } // Do nothing

		public void Normalize(float[] sample) { } // Do nothing
	}
}
