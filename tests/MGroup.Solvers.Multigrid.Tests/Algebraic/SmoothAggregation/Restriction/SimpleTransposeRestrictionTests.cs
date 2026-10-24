namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Restriction
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SimpleTransposeRestrictionTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			double dampingFactor = 4.0 / 3.0;
			int degree = 1;
			CsrMatrix P = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(level: 0, weightingStrategy: "SpectralRadius", dampingFactor, degree);
			CsrMatrix expectedR = Poisson1D_12Elements_SaAmgResults.GetRestrictionMatrix(level: 0);

			var restrictionStrategy = new SimpleTransposeRestriction();
			CsrMatrix computedR = restrictionStrategy.CalcRestrictionMatrix(P);

			AssertUtilities.AssertEqualCsr(expectedR, computedR, tolerance: 1E-20);
		}
	}
}
