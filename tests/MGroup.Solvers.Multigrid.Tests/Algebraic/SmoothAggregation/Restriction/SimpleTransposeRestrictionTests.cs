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
			double weightFactor = 4.0 / 3.0;
			int degree = 1;
			CsrMatrix P = Poisson1D12ElementsSaAmgResults.GetProlongationMatrix(level: 0, weightFactor, degree);
			CsrMatrix expectedR = Poisson1D12ElementsSaAmgResults.GetRestrictionMatrix(level: 0);

			var restrictionStrategy = new SimpleTransposeRestriction();
			CsrMatrix computedR = restrictionStrategy.CalcRestrictionMatrix(P);

			AssertUtilities.AssertEqualCsr(expectedR, computedR, tolerance: 1E-20);
		}
	}
}
