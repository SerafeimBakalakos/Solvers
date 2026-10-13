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
			double[,] C = Poisson1D12ElementsSaAmgResults.GetStrenghOfConnectionMatrix(level: 0, theta: 0);
			CsrMatrix soc = MatrixUtilities.ArrayToCsr(C);

			var aggregationStrategy = new StandardAggregation();
			AggregateCollection aggregates = aggregationStrategy.FindAggregates(soc);

			int numAggregatesExpected = Poisson1D12ElementsSaAmgResults.SystemOrders[1];
			(int[] aggregatesOfFineDofsExpected, int[] fineDofRootsOfAggregatesExpected, _) = Poisson1D12ElementsSaAmgResults.GetAggregates(level: 0);

			Assert.Equal(numAggregatesExpected, aggregates.NumAggregates);
			Assert.Equal(aggregatesOfFineDofsExpected, aggregates.AggregatesOfFineDofs);
			Assert.Equal(fineDofRootsOfAggregatesExpected, aggregates.FineDofRootsOfAggregates);
		}
	}
}
