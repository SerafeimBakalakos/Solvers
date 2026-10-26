namespace MGroup.Solvers.Multigrid.Tests.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseMatrix;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class GalerkinProductCsrTests
	{
		[Fact]
		public static void TestCalcProduct_Cantilever8x4Elements()
		{
			var galerkin = new GalerkinProductCsr();

			// Expected
			CsrMatrix expectedA0 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(0);
			CsrMatrix expectedA1 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(1);
			CsrMatrix expectedA2 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(2);

			// Level 0
			CsrMatrix P0 = Cantilever2D_8x4Elements_SaAmgResults.GetProlongation(0);
			CsrMatrix R0 = Cantilever2D_8x4Elements_SaAmgResults.GetRestriction(0);
			CsrMatrix computedA1 = galerkin.CalcProduct(R0, expectedA0, P0);
			AssertUtilities.AssertEqualCsr(expectedA1, computedA1, tolerance: 1E-13);

			// Level 1
			CsrMatrix P1 = Cantilever2D_8x4Elements_SaAmgResults.GetProlongation(1);
			CsrMatrix R1 = Cantilever2D_8x4Elements_SaAmgResults.GetRestriction(1);
			CsrMatrix computedA2 = galerkin.CalcProduct(R1, expectedA1, P1);
			AssertUtilities.AssertEqualCsr(expectedA2, computedA2, tolerance: 1E-13);
		}

		[Fact]
		public static void TestCalcProduct_Poisson12Elements()
		{
			CsrMatrix P = Poisson1D_12Elements_SaAmgResults.GetProlongationMatrix(0, dampingFactor: 4.0 / 3.0, degree: 1);
			CsrMatrix R = Poisson1D_12Elements_SaAmgResults.GetRestrictionMatrix(0);
			CsrMatrix Af = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(0);
			CsrMatrix expectedAc = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(1);

			var galerkin = new GalerkinProductCsr();
			CsrMatrix Ac = galerkin.CalcProduct(R, Af, P);

			AssertUtilities.AssertEqualCsr(expectedAc, Ac, tolerance: 1E-15);
		}
	}
}
