using MGroup.Solvers.MachineLearning.StochasticExtensions.Statistics;

namespace MGroup.Solvers.MachineLearning.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using DotNumerics.Optimization;

	public class RandomHomogeneousField : IRandomField1D
	{
		private readonly double variableMean;
		private readonly double variableStdDev;
		private readonly IDistribution distribution;

		private double sample;

		public RandomHomogeneousField(double variableMean, double variableStdDev, Random rng,
			bool useLogNormalDistribution)
		{
			this.variableMean = variableMean;
			this.variableStdDev = variableStdDev;

			this.distribution = useLogNormalDistribution
				? LogNormalDistribution.CreateWithMeanStddev(rng, variableMean, variableStdDev)
				: NormalDistribution.CreateWithMeanStddev(rng, variableMean, variableStdDev);
		}

		public int NumParameters => 1;

		public double CalcValueAt(double coords) => sample;

		public void Initialize() { }

		public double[] Regenerate()
		{
			sample = distribution.GenerateSample();
			return new double[] {sample };
		}
	}
}
