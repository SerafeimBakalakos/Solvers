namespace MGroup.Solvers.MachineLearning.Tests.StatisticsExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MathNet.Numerics.Distributions;

	using Tensorflow.Keras.Metrics;

	public class LogNormalDistribution : IDistribution
	{
		private readonly double mu; // not the mean
		private readonly double sigma; // not the standard deviation
		private readonly Random rng;

		private LogNormalDistribution(Random rng, double mu, double sigma)
		{
			this.rng = rng;
			this.mu = mu;
			this.sigma = sigma;
		}

		public static LogNormalDistribution CreateWithLognormalParams(Random rng, double mu, double sigma)
			=> new LogNormalDistribution(rng, mu, sigma);

		public static LogNormalDistribution CreateWithMeanStddev(Random rng, double mean, double standardDeviation)
		{
			double m2 = mean * mean;
			double s2 = standardDeviation * standardDeviation;
			double mu = Math.Log(m2 / Math.Sqrt(m2 + s2));
			double sigma = Math.Sqrt(Math.Log(s2 / m2 + 1));
			return new LogNormalDistribution(rng, mu, sigma);
		}

		public double GenerateSample() => Math.Exp(mu + sigma * Normal.Sample(rng, 0.0, 1.0));
	}
}
