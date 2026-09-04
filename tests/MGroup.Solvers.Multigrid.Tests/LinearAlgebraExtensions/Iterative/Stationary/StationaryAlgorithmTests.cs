namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Iterative.Stationary
{
	using MGroup.LinearAlgebra.Iterative.Termination.Convergence;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Output;
	//using MGroup.LinearAlgebra.Tests.TestData;
	//using MGroup.LinearAlgebra.Tests.Utilities;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class StationaryAlgorithmTests
	{
		[Fact]
		private static void TestGaussSeidelBackSolution()
		{
			IStationaryIteration stationaryIteration = new GaussSeidelIterationCsr(forwardDirection: false);
			int numIterations = 11;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-11;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestGaussSeidelForwardSolution()
		{
			IStationaryIteration stationaryIteration = new GaussSeidelIterationCsr(forwardDirection: true);
			int numIterations = 10;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-10;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestGaussSeidelSymmetricSolution()
		{
			IStationaryIteration stationaryIteration = new SgsIterationCsr();
			int numIterations = 10;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-10;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestJacobiSolution()
		{
			IStationaryIteration stationaryIteration = new JacobiIterationCsr();
			int numIterations = 27;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-9;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestJacobiWeightedSolution()
		{
			double omega = 0.9;
			IStationaryIteration stationaryIteration = new WeightedJacobiIterationCsr(omega);
			int numIterations = 22;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-9;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestSorBackSolution()
		{
			double omega = 1.1;
			IStationaryIteration stationaryIteration = new SorIterationCsr(omega, forwardDirection: false);
			int numIterations = 13;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-5;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestSorForwardSolution()
		{
			double omega = 1.1;
			IStationaryIteration stationaryIteration = new SorIterationCsr(omega, forwardDirection: true);
			int numIterations = 13;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-5;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		[Fact]
		private static void TestSsorSolution()
		{
			double omega = 1.1;
			IStationaryIteration stationaryIteration = new SorIterationCsr(omega, forwardDirection: true);
			int numIterations = 13;
			double convergenceTolerance = 1E-9;
			double entrywiseTolerance = 1E-5;
			TestSystemSolution(stationaryIteration, numIterations, convergenceTolerance, entrywiseTolerance);
		}

		private static void TestSystemSolution(IStationaryIteration stationaryIteration, int numIterations,
			double convergenceTolerance, double entrywiseTolerance)
		{
			var matrix = CsrMatrix.CreateFromArrays(SparsePosDef10by10.Order, SparsePosDef10by10.Order,
					SparsePosDef10by10.CsrValues, SparsePosDef10by10.CsrColIndices, SparsePosDef10by10.CsrRowOffsets, true);
			var b = Vector.CreateFromArray(SparsePosDef10by10.Rhs);
			var xExpected = Vector.CreateFromArray(SparsePosDef10by10.Lhs);

			var builder = new StationaryIterativeMethod.Factory(stationaryIteration);
			//builder.ConvergenceCriterion = new SolutionNeverConvergesCriterion(); // We would use this, but to test we need to track the convergence rate.
			builder.ConvergenceCriterion = new AbsoluteSolutionConvergenceCriterion();
			builder.ConvergenceTolerance = 0.0;
			builder.MaxIterationsProvider = new FixedMaxIterationsProvider(numIterations);
			var gs = builder.Build();
			var xComputed = Vector.CreateZero(b.Length);

			var stats = gs.Solve(matrix, b, xComputed);

			Assert.Equal(numIterations, stats.NumIterationsRequired);
			Assert.InRange(stats.ConvergenceCriterion.value, 0.0, convergenceTolerance);

			var comparer = new MatrixComparer(entrywiseTolerance);
			comparer.AssertEqual(xExpected, xComputed);
		}
	}
}
