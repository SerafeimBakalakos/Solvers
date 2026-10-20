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
		[Theory]
		[InlineData(0.0)]
		[InlineData(0.25)]
		[InlineData(0.5)]
		[InlineData(0.75)]
		[InlineData(1.0)]
		public static void TestForPoisson1D(double theta)
		{
			(DokRowMajor dokA, _) = Poisson1DProblem.CreateLinearSystemWithConstantSource(numElements: 12, source: 1.0);
			CsrMatrix csrA = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);

			var soc = new SymmetricStrengthOfConnection(theta);
			DokRowMajor computedStrengthDok = soc.Compute(dokA);
			CsrMatrix computedStrengthCsr = soc.CalcStrengthMatrix(csrA);

			double[,] expectedStrength = Poisson1D12ElementsSaAmgResults.GetStrengthOfConnectionMatrix(level: 0, theta);
 			DokRowMajor expectedStrengthDok = MatrixUtilities.ArrayToDok(expectedStrength);
			CsrMatrix expectedStrengthCsr = MatrixUtilities.ArrayToCsr(expectedStrength);

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedStrengthDok, computedStrengthDok);
			AssertUtilities.AssertEqualCsr(expectedStrengthCsr, computedStrengthCsr);
		}
	}
}
