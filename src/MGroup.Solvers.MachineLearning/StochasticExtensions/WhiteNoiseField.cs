namespace MGroup.Solvers.MachineLearning.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.Statistics;

	public class WhiteNoiseField : IRandomField
	{
		private readonly double variableMean;
		private readonly double variableStdDev;
		private readonly int numSamples;
		private readonly IDistribution distribution;
		
		private double[] samples;
		private int numSamplesUsed;

		public WhiteNoiseField(double variableMean, double variableStdDev, int numSamples, Random rng, 
			bool useLogNormalDistribution)
		{
			this.variableMean = variableMean;
			this.variableStdDev = variableStdDev;
			this.numSamples = numSamples;

			this.distribution = useLogNormalDistribution
				? LogNormalDistribution.CreateWithMeanStddev(rng, variableMean, variableStdDev)
				: NormalDistribution.CreateWithMeanStddev(rng, variableMean, variableStdDev);
		}

		private static void PrintSamples(double[] samples) 
		{
			Console.WriteLine("Samples: ");
			for (int i = 0; i < samples.Length; i++)
			{
				Console.WriteLine($"{samples[i]}");
			}
			Console.WriteLine();
		}

		public double CalcValueAt(double[] coords)
		{
			if (numSamplesUsed < numSamples)
			{
				double result = samples[numSamplesUsed];
				numSamplesUsed++;
				return result;
			}
			else
			{
				throw new InvalidOperationException(
					"Available samples have been exhausted. Use \"field.Regenerate()\" for more.");
			}
		}

		public void Initialize() { }
		
		public double[] Regenerate()
		{
			numSamplesUsed = 0;
			samples = distribution.GenerateSamples(numSamples);

			#region debug
			//PrintSamples(samples);
			#endregion

			return samples;
		}
	}
}
