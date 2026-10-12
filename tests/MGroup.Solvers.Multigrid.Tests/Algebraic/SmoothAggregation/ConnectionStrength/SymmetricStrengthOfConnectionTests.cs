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
			int numElements = 12;
			(DokRowMajor dokA, Vector b) = Poisson1DProblem.CreateWithConstantSource(numElements, source: 1.0);
			CsrMatrix csrA = dokA.BuildCsrMatrix(sortColsOfEachRow: true);

			var soc = new SymmetricStrengthOfConnection(theta);
			DokRowMajor computedStrengthDok = soc.Compute(dokA);
			CsrMatrix computedStrengthCsr = soc.CalcStrengthMatrix(csrA);

			DokRowMajor expectedStrengthDok = MatrixUtilities.ArrayToDok(GetSocMatrixForPoisson1D(numElements, theta));
			CsrMatrix expectedStrengthCsr = MatrixUtilities.ArrayToCsr(GetSocMatrixForPoisson1D(numElements, theta));

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedStrengthDok, computedStrengthDok);
			AssertUtilities.AssertEqualCsr(expectedStrengthCsr, computedStrengthCsr);
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
