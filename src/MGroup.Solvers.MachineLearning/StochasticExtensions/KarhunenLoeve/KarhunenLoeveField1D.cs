namespace MGroup.Solvers.MachineLearning.StochasticExtensions.KarhunenLoeve
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class KarhunenLoeveField1D : IRandomField1D
	{
		private readonly double domainMin;
		private readonly double domainMax;
		private readonly int numNodes;
		private readonly double nodeDistance;
		private readonly double fieldMean;
		private readonly double fieldStdDev;
		private readonly double correlationLength;
		private readonly int numKarLoeveTerms;
		private readonly int rngSeed;

		private readonly KarhunenLoeve1DCoefficientsProvider klProvider;
		private double[] fieldAtNodes;

		public int NumParameters => numKarLoeveTerms;

		public KarhunenLoeveField1D(double domainMin, double domainMax, int numNodes, double fieldMean, double fieldStdDev,
			double correlationLength, int numKarLoeveTerms, Random rng)
		{
			this.numNodes = numNodes;
			this.nodeDistance = (domainMax - domainMin) / (numNodes - 1);
			this.numKarLoeveTerms = numKarLoeveTerms;
			klProvider = new KarhunenLoeve1DCoefficientsProvider(new double[] { domainMin, domainMax }, numNodes,
				fieldMean, fieldStdDev, correlationLength, numKarLoeveTerms, rng, true, true);
		}

		public double CalcValueAt(double coords)
		{
			var xOverDx = coords / nodeDistance;
			var previousNodeIdx = (int)xOverDx;
			if (previousNodeIdx != numNodes - 1)
			{
				// Some point prior to last node. Use interpolation. 
				//TODO: For now I do linear interpolation. Instead I should read the interpolation order from KL provider
				var xi = xOverDx - previousNodeIdx; // xi=0 at previous node, xi=1 at next node
				return fieldAtNodes[previousNodeIdx] * (1 - xi) + fieldAtNodes[previousNodeIdx + 1] * xi;
			}
			else
			{
				// Last node
				return fieldAtNodes[numNodes - 1];
			}
		}

		public void Initialize() => klProvider.Initialize();

		public double[] Regenerate()
		{
			klProvider.ResetSampleGeneration();
			double[] parameters = CopyArray(klProvider.Ksi);
			fieldAtNodes = klProvider.GenerateFieldAtNodes();
			return parameters;
		}

		private static double[] CopyArray(double[] original)
		{
			var result = new double[original.Length];
			Array.Copy(original, result, original.Length);
			return result;
		}
	}
}
