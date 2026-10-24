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
		public static void TestWithSpectralRadiusWeightingForPoisson1D(double dampingFactor, int degree, double tolerance)
		{
			CsrMatrix systemMatrix = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			CsrMatrix tentativeP = Poisson1D_12Elements_SaAmgResults.GetTentativeProlongation(level: 0, relaxationStrategy: "SGS-4");

			IJacobiWeightingStrategy weightingStrategy = new SpectralRadiusJacobiWeighting(dampingFactor, new SystemRandomRng(seed: 13));
			var prolongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, degree);
			CsrMatrix computedP = prolongationSmoothing.SmoothProlongationMatrix(tentativeP, systemMatrix);

			CsrMatrix expectedP = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(level: 0, weightingStrategy:"SpectralRadius", dampingFactor, degree);
			AssertUtilities.AssertEqualCsr(expectedP, computedP, tolerance);
		}

		[Fact]
		public static void TestWithGershgorinWeightingForPoisson1D()
		{
			CsrMatrix systemMatrix = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			CsrMatrix tentativeP = Poisson1D_12Elements_SaAmgResults.GetTentativeProlongation(level: 0, relaxationStrategy: "SGS-4");

			IJacobiWeightingStrategy weightingStrategy = new GershgorinJacobiWeighting(dampingFactor: 4.0 / 3);
			var prolongationSmoothing = new JacobiProlongationSmoothing(weightingStrategy, degree: 1);
			CsrMatrix computedP = prolongationSmoothing.SmoothProlongationMatrix(tentativeP, systemMatrix);

			CsrMatrix expectedP = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(level: 0, weightingStrategy: "Gershgorin", dampingFactor: 4.0 / 3.0, degree: 1);
			AssertUtilities.AssertEqualCsr(expectedP, computedP, 1E-30);
		}
	}
}
