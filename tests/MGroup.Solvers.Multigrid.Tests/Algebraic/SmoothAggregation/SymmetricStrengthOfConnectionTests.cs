namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
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
			int numElements = 12;
			(DokRowMajor A, Vector b) = Poisson1DProblem.CreateWithConstantSource(numElements, source: 1.0);
			DokRowMajor socComputed = SymmetricStrengthOfConnection.Compute(A, theta);

			DokRowMajor socExpected = MatrixUtilities.ArrayToDok(GetSocMatrixForPoisson1D(numElements, theta));

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(socExpected, socComputed);
		}

		internal static double[,] GetSocMatrixForPoisson1D(int numElements, double theta)
		{
			if (numElements == 12)
			{
				if (theta == 0.0 || theta == 0.25 || theta == 0.5)
				{
					return new double[,]
					{
						{ 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0 }
					};
				}
				else if (theta == 0.75 || theta == 1.0)
				{
					return MatrixUtilities.CreateIdentityAsArray(numElements - 1);
				}
			}

			throw new NotImplementedException();
		}
	}
}
