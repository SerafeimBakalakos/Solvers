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
		public static AggregateCollection GetAggregatesForPoisson1D(int numElements, double theta)
		{
			if (numElements == 12 && theta == 0)
			{
				int[] aggregatesOfFineDofsExpected = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
				int[] fineDofRootsOfAggregatesExpected = [0, 3, 6, 9];
				return new AggregateCollection(aggregatesOfFineDofsExpected, fineDofRootsOfAggregatesExpected);
			}
			else throw new NotImplementedException();
		}

		[Fact]
		public static void TestForPoisson1D()
		{
			int numElements = 12;
			double theta = 0;

			double[,] C = SymmetricStrengthOfConnectionTests.GetSocMatrixForPoisson1D(numElements, theta);
			CsrMatrix soc = MatrixUtilities.ArrayToCsr(C);

			var aggregationStrategy = new StandardAggregation();
			AggregateCollection aggregates = aggregationStrategy.FindAggregates(soc);
			AggregateCollection expected = GetAggregatesForPoisson1D(numElements, theta);

			Assert.Equal(expected.NumAggregates, aggregates.NumAggregates);
			Assert.Equal(expected.AggregatesOfFineDofs, aggregates.AggregatesOfFineDofs);
			Assert.Equal(expected.FineDofRootsOfAggregates, aggregates.FineDofRootsOfAggregates);
		}
	}
}
