namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Tests.Examples;

	using Xunit;

	public static class SystemOrderBasedTerminationTests
	{
		[Fact]
		public static void TestMustStopCoarsening_Cantilever8x4Elements()
		{
			var terminationCriterion = new SystemSizeBasedTermination(targetMinSize: 10);

			CsrMatrix A0 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 0);
			Assert.Equal(false, terminationCriterion.MustStopCoarsening(A0));

			CsrMatrix A1 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 1);
			Assert.Equal(false, terminationCriterion.MustStopCoarsening(A1));

			CsrMatrix A2 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 2);
			Assert.Equal(true, terminationCriterion.MustStopCoarsening(A2));
		}

		[Fact]
		public static void TestMustStopCoarsening_Poisson12Elements()
		{
			var terminationCriterion = new SystemSizeBasedTermination(targetMinSize: 10);

			CsrMatrix Af = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			Assert.Equal(false, terminationCriterion.MustStopCoarsening(Af));

			CsrMatrix Ac = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 1);
			Assert.Equal(true, terminationCriterion.MustStopCoarsening(Ac));
		}
	}
}
