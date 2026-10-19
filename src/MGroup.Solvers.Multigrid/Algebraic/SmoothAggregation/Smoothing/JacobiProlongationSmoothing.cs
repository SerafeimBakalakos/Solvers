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
		private readonly IJacobiWeightingStrategy weighting;
		private readonly int degree;

		public JacobiProlongationSmoothing(IJacobiWeightingStrategy weighting, int degree = 1)
		{
			if (degree < 1)
			{
				throw new ArgumentException("Degree must be positive integer");
			}

			this.weighting = weighting;
			this.degree = degree;
		}

		public CsrMatrix SmoothProlongationMatrix(CsrMatrix tentativeProlongationMatrix, CsrMatrix systemMatrix)
		{
			CsrMatrix weightedA = weighting.WeightSystemMatrix(systemMatrix);
			CsrMatrix P = tentativeProlongationMatrix;
			for (int k = 0; k < degree; k++) // Smooth the prolongation matrix one or more times
			{
				CscMatrix colMajorP = Conversions.CsrToCsc(P);
				CsrMatrix temp = CsrCscMultiplications.CsrTimesCscToCsr(weightedA, colMajorP);
				P = CsrCsrBinaryOperations.CsrMinusCsrCanonical(P, temp);
			}

			return P;
		}
	}
}
