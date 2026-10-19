namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.EigenSystems;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;

	public class SpectralRadiusJacobiWeighting : IJacobiWeightingStrategy
	{
		private readonly double dampingFactor;
		private readonly IRandomNumberGenerator rng;

		public SpectralRadiusJacobiWeighting(double dampingFactor = 4.0 / 3.0, IRandomNumberGenerator? rng = null)
		{
			if (dampingFactor <= 0 || dampingFactor > 2)
			{
				throw new ArgumentException("Weight factor must be in (0, 2]");
			}

			this.dampingFactor = dampingFactor;
			this.rng = (rng is null) ? new SystemRandomRng() : rng;
		}

		public CsrMatrix WeightSystemMatrix(CsrMatrix systemMatrix)
		{
			// Construct inv(D) * A without modifying the original system matrix.
			double[] diagonal = systemMatrix.GetDiagonalAsArray();
			CsrMatrix weightedMatrix = systemMatrix.CopyAsSameType(copyIndexingData: false);
			weightedMatrix.ScaleRows(diagonal, invertDiagonal: true);

			// Estimate the spectral radius of inv(D) * A and apply the Jacobi damping factor.
			var spectralRadius = new SpectralRadius(tolerance: 0.01, maxIterations: 15, restart: 5, rng);
			double rho = spectralRadius.ApproximateSpectralRadius(weightedMatrix);
			weightedMatrix.ScaleIntoThis(dampingFactor / rho);

			return weightedMatrix;
		}
	}
}
