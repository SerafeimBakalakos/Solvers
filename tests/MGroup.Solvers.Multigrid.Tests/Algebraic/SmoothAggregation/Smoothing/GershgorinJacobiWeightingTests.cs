namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class GershgorinJacobiWeightingTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			CsrMatrix systemMatrix = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			IJacobiWeightingStrategy weightingStrategy = new GershgorinJacobiWeighting(dampingFactor: 4.0 / 3);
			CsrMatrix computed = weightingStrategy.WeightSystemMatrix(systemMatrix);

			CsrMatrix expected = Poisson1D_12Elements_SaAmgResults.GetSystemMatrixWeighted(level: 0, weightingStrategy: "Gershgorin");
			AssertUtilities.AssertEqualCsr(expected, computed, 1E-30);
		}
	}
}
