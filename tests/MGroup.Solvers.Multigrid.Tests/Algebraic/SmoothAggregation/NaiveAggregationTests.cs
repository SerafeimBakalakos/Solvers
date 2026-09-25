namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class NaiveAggregationTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			double[,] C = SymmetricStrengthOfConnectionTests.GetSocMatrixForPoisson1D(numElements: 12, theta: 0);
			CsrMatrix soc = MatrixUtilities.ArrayToCsr(C);

			var aggregationStrategy = new NaiveAggregation();
			AggregateCollection aggregates = aggregationStrategy.FindAggregates(soc);

			int numAggregatesExpected = 6;
			int[] aggregatesOfFineDofsExpected = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5];
			int[] fineDofRootsOfAggregatesExpected = [0, 2, 4, 6, 8, 10];

			Assert.Equal(numAggregatesExpected, aggregates.NumAggregates);
			Assert.Equal(aggregatesOfFineDofsExpected, aggregates.AggregatesOfFineDofs);
			Assert.Equal(fineDofRootsOfAggregatesExpected, aggregates.FineDofRootsOfAggregates);
		}
	}
}
