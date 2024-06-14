namespace MGroup.Solvers.MachineLearning.Tests.StatisticsExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MathNet.Numerics.Distributions;

	public class NormalDistribution : IDistribution
	{
		private readonly double mean;
		private readonly double stddev;
		private readonly Random rng;

		private NormalDistribution(Random rng, double mean, double stddev)
		{
			this.rng = rng;
			this.mean = mean;
			this.stddev = stddev;
		}

		public static NormalDistribution CreateStandard(Random rng) => new NormalDistribution(rng, 0.0, 1.0);

		public static NormalDistribution CreateWithMeanStddev(Random rng, double mean, double standardDeviation)
			=> new NormalDistribution(rng, mean, standardDeviation);

		public double GenerateSample() => Normal.Sample(rng, mean, stddev);
	}
}
