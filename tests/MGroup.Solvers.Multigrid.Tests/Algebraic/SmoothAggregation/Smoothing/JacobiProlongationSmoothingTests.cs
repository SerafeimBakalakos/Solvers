namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.EigenSystems;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class JacobiProlongationSmoothingTests
	{
		[Theory]
		[InlineData(4.0 / 3.0, 1, 1E-14)]
		[InlineData(1.2, 3, 1E-14)]
		public static void TestForPoisson1D(double weightFactor, int degree, double tolerance)
		{
			CsrMatrix systemMatrix = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);
			CsrMatrix tentativeP = Poisson1D12ElementsSaAmgResults.GetTentativeProlongation(level: 0, relaxationStrategy: "SGS-4");

			var rng = new SystemRandomRng(seed: 13);
			var smoothing = new JacobiProlongationSmoothing(weightFactor, degree, rng);
			CsrMatrix computedP = smoothing.SmoothProlongationMatrix(tentativeP, systemMatrix);

			CsrMatrix expectedP = Poisson1D12ElementsSaAmgResults.GetProlongationMatrix(level: 0, weightFactor, degree);
			AssertUtilities.AssertEqualCsr(expectedP, computedP, tolerance);
		}
	}
}
