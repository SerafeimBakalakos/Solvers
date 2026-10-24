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
		public static void TestCalcRestrictionMatrix_Cantilever8x4Elements()
		{
			var restrictionStrategy = new SimpleTransposeRestriction();

			// Level 0
			CsrMatrix P0 = Cantilever2D_8x4Elements_SaAmgResults.GetProlongation(level: 0);
			CsrMatrix R0 = restrictionStrategy.CalcRestrictionMatrix(P0);

			CsrMatrix expectedR0 = Cantilever2D_8x4Elements_SaAmgResults.GetRestriction(level: 0);
			AssertUtilities.AssertEqualCsr(expectedR0, R0, tolerance: 1E-30);

			// Level 1
			CsrMatrix P1 = Cantilever2D_8x4Elements_SaAmgResults.GetProlongation(level: 1);
			CsrMatrix R1 = restrictionStrategy.CalcRestrictionMatrix(P1);

			CsrMatrix expectedR1 = Cantilever2D_8x4Elements_SaAmgResults.GetRestriction(level: 1);
			AssertUtilities.AssertEqualCsr(expectedR1, R1, tolerance: 1E-30);
		}

		[Fact]
		public static void TestCalcRestrictionMatrix_Poisson12Elements()
		{
			double dampingFactor = 4.0 / 3.0;
			int degree = 1;
			CsrMatrix P = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(level: 0, weightingStrategy: "SpectralRadius", dampingFactor, degree);
			CsrMatrix expectedR = Poisson1D_12Elements_SaAmgResults.GetRestrictionMatrix(level: 0);

			var restrictionStrategy = new SimpleTransposeRestriction();
			CsrMatrix computedR = restrictionStrategy.CalcRestrictionMatrix(P);

			AssertUtilities.AssertEqualCsr(expectedR, computedR, tolerance: 1E-30);
		}
	}
}
