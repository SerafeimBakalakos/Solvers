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
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseMatrix;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class GalerkinProductCsrTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			CsrMatrix P = Poisson1D12ElementsSaAmgResults.GetProlongationMatrix(0, weightFactor: 4.0 / 3.0, degree: 1);
			CsrMatrix R = Poisson1D12ElementsSaAmgResults.GetRestrictionMatrix(0);
			CsrMatrix Af = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(0);
			CsrMatrix expectedAc = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(1);

			var galerkin = new GalerkinProductCsr();
			CsrMatrix Ac = galerkin.CalcProduct(R, Af, P);

			AssertUtilities.AssertEqualCsr(expectedAc, Ac, tolerance: 1E-15);
		}
	}
}
