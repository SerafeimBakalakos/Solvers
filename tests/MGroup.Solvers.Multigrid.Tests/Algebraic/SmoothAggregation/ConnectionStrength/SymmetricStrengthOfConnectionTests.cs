namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.ConnectionStrength
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SymmetricStrengthOfConnectionTests
	{
		[Fact]
		public static void TestGetStrengthOfConnectionMatrix_Cantilever8x4Elements()
		{
			var soc = new SymmetricStrengthOfConnection(theta: 0);

			// Level 0
			CsrMatrix A0 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 0);
			CsrMatrix C0 = soc.CalcStrengthMatrix(A0, blockSize: 1);

			CsrMatrix expectedC0 = Cantilever2D_8x4Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 0);
			AssertUtilities.AssertEqualCsr(expectedC0, C0, tolerance: 1E-15);

			// Level 1
			CsrMatrix A1 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 1);
			CsrMatrix C1 = soc.CalcStrengthMatrix(A1, blockSize: 3);

			CsrMatrix expectedC1 = Cantilever2D_8x4Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 1);
			AssertUtilities.AssertEqualCsr(expectedC1, C1, tolerance: 1E-30);
		}

		[Theory]
		[InlineData(0.0)]
		[InlineData(0.25)]
		[InlineData(0.5)]
		[InlineData(0.75)]
		[InlineData(1.0)]
		public static void TestGetStrengthOfConnectionMatrix_Poisson1D12Elements(double theta)
		{
			(DokRowMajor dokA, _) = Poisson1DProblem.CreateLinearSystemWithConstantSource(numElements: 12, source: 1.0);
			CsrMatrix csrA = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);

			var soc = new SymmetricStrengthOfConnection(theta);
			DokRowMajor computedStrengthDok = soc.ComputeScalar(dokA);
			CsrMatrix computedStrengthCsr = soc.CalcStrengthMatrix(csrA, blockSize: 1);

			double[,] expectedStrength = Poisson1D_12Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 0, theta);
 			DokRowMajor expectedStrengthDok = MatrixUtilities.ArrayToDok(expectedStrength);
			CsrMatrix expectedStrengthCsr = MatrixUtilities.ArrayToCsr(expectedStrength);

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedStrengthDok, computedStrengthDok);
			AssertUtilities.AssertEqualCsr(expectedStrengthCsr, computedStrengthCsr);
		}
	}
}
