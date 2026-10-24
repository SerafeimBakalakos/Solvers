namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class StandardAggregationTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			double[,] C = Poisson1D_12Elements_SaAmgResults.GetStrengthOfConnectionMatrix(level: 0, theta: 0);
			CsrMatrix soc = MatrixUtilities.ArrayToCsr(C);

			var aggregationStrategy = new StandardAggregation();
			AggregateCollection aggregates = aggregationStrategy.FindAggregates(soc);

			int numAggregatesExpected = Poisson1D_12Elements_SaAmgResults.SystemOrders[1];
			(int[] aggregatesOfGraphNodesExpected, int[] rootGraphNodesOfAggregatesExpected, _) = Poisson1D_12Elements_SaAmgResults.GetAggregates(level: 0);

			Assert.Equal(numAggregatesExpected, aggregates.NumAggregates);
			Assert.Equal(aggregatesOfGraphNodesExpected, aggregates.AggregatesOfGraphNodes);
			Assert.Equal(rootGraphNodesOfAggregatesExpected, aggregates.RootGraphNodesOfAggregates);
		}
	}
}
