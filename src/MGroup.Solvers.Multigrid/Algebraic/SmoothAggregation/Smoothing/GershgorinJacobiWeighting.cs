namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;

	public class GershgorinJacobiWeighting : IJacobiWeightingStrategy
	{
		private readonly double dampingFactor;

		public GershgorinJacobiWeighting(double dampingFactor = 4.0 / 3.0)
		{
			if (dampingFactor <= 0 || dampingFactor > 2)
			{
				throw new ArgumentException("Weight factor must be in (0, 2]");
			}

			this.dampingFactor = dampingFactor;
		}

		public CsrMatrix WeightSystemMatrix(CsrMatrix systemMatrix)
		{
			double[] rowSums = systemMatrix.CalcAbsRowSums();
			var rowWeights = new double[systemMatrix.NumRows];

			for (int row = 0; row < systemMatrix.NumRows; ++row)
			{
				double sum = rowSums[row];
				if (sum > 0.0)
				{
					rowWeights[row] = dampingFactor / sum;
				}
			}

			// Apply the row weights without modifying the original system matrix.
			CsrMatrix weightedMatrix = systemMatrix.CopyAsSameType(copyIndexingData: false);
			weightedMatrix.ScaleRows(rowWeights, invertDiagonal: false);

			return weightedMatrix;
		}
	}
}
