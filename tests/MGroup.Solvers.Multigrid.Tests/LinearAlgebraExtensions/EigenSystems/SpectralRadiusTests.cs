namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.EigenSystems
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.EigenSystems;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.TestData;

	using Xunit;

	public static class SpectralRadiusTests
	{
		[Fact]
		public static void TestMaxEigenvalueOfPoisson1DSystemMatrix()
		{
			int n = Poisson1D_12Elements.SystemOrder;
			var matrix = CsrMatrix.CreateFromArrays(n, n, Poisson1D_12Elements.SystemMatrixCsrValues, Poisson1D_12Elements.SystemMatrixCsrColIndices, Poisson1D_12Elements.SystemMatrixCsrRowOffsets, checkInput: true);

			var rng = new SystemRandomRng(seed: 13);
			var spectralRadius = new SpectralRadius(tolerance: 0.01, maxIterations: 15, restart: 5, rng);
			double lambdaMax = spectralRadius.ApproximateSpectralRadius(matrix);

			double lambdaMaxExpected = Poisson1D_12Elements.SystemMatrixMaxEigenvalue;
			Assert.Equal(lambdaMaxExpected, lambdaMax, tolerance: 1E-12);
		}

		[Fact]
		public static void TestMaxEigenvalueOfPoisson1DNormalizedSystemMatrix()
		{
			var matrix = CsrMatrix.CreateFromArrays(Poisson1D_12Elements.SystemOrder, Poisson1D_12Elements.SystemOrder, Poisson1D_12Elements.SystemMatrixNormalizedCsrValues, Poisson1D_12Elements.SystemMatrixCsrColIndices, Poisson1D_12Elements.SystemMatrixCsrRowOffsets, checkInput: true);

			var rng = new SystemRandomRng(seed: 13);
			var spectralRadius = new SpectralRadius(tolerance: 0.01, maxIterations: 15, restart: 5, rng);
			double lambdaMax = spectralRadius.ApproximateSpectralRadius(matrix);

			double lambdaMaxExpected = Poisson1D_12Elements.SystemMatrixNormalizedMaxEigenvalue;
			Assert.Equal(lambdaMaxExpected, lambdaMax, tolerance: 1E-12);
		}
	}
}
