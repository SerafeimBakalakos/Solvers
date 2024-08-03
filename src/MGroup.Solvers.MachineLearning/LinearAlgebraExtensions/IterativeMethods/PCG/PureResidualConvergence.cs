using System;
using System.Collections.Generic;
using System.Text;

using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;

namespace MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.IterativeMethods.PCG
{
	public class PureResidualConvergence : IPcgResidualConvergence
	{
		private double denominator;

		public IPcgResidualConvergence CopyWithInitialSettings() => new PureResidualConvergence();

		public double EstimateResidualNormRatio(PcgAlgorithmBase pcg) => pcg.Residual.Norm2() / denominator;

		public void Initialize(PcgAlgorithmBase pcg) => denominator = pcg.Rhs.Norm2();
	}
}
