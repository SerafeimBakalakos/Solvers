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
		public static void TestPoisson1D()
		{
			var terminationCriterion = new SystemSizeBasedTermination(targetMinSize: 10);

			CsrMatrix Af = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);
			Assert.Equal(false, terminationCriterion.MustStopCoarsening(Af));

			CsrMatrix Ac = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 1);
			Assert.Equal(true, terminationCriterion.MustStopCoarsening(Ac));
		}
	}
}
