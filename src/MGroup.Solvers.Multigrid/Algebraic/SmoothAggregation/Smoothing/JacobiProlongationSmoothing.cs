namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.LinearAlgebraExtensions.EigenSystems;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;

	public class JacobiProlongationSmoothing : IProlongationSmoothingStrategy
	{
		private readonly double weightFactor;
		private readonly int degree;
		private readonly IRandomNumberGenerator rng;

		public JacobiProlongationSmoothing(double weightFactor = 4.0 / 3.0, int degree = 1, IRandomNumberGenerator? rng = null)
		{
			if (weightFactor <= 0 || weightFactor > 2)
			{
				throw new ArgumentException("Weight factor must be in (0, 2]");
			}

			if (degree < 1)
			{
				throw new ArgumentException("Degree must be positive integer");
			}

			this.weightFactor = weightFactor;
			this.degree = degree;

			this.rng = (rng is null) ? new SystemRandomRng() : rng;
		}

		public CsrMatrix SmoothProlongationMatrix(CsrMatrix tentativeProlongationMatrix, CsrMatrix systemMatrix)
		{
			// Extract diagonal D of system matrix A
			double[] diagonal = systemMatrix.GetDiagonalAsArray();

			// Normalize S = inv(D) * A
			CsrMatrix invDTimesA = systemMatrix.CopyAsSameType(copyIndexingData: false);
			invDTimesA.ScaleRows(diagonal, invertDiagonal: true);

			// Approximate spectral radius of S
			var sr = new SpectralRadius(tolerance: 0.01, maxIterations: 15, restart: 5, rng);
			double rho = sr.ApproximateSpectralRadius(invDTimesA);

			// Smooth the prolongation matrix one or more times
			CsrMatrix P = tentativeProlongationMatrix;
			invDTimesA.ScaleIntoThis(weightFactor / rho);
			for (int k = 0; k < degree; k++)
			{
				CscMatrix colMajorP = Conversions.CsrToCsc(P);
				CsrMatrix temp = CsrCscMultiplications.CsrTimesCscToCsr(invDTimesA, colMajorP);
				P = CsrCsrBinaryOperations.CsrMinusCsrCanonical(P, temp);
			}

			return P;
		}
	}
}
