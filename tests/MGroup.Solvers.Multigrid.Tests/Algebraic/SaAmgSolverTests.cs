namespace MGroup.Solvers.Multigrid.Tests.Algebraic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearAlgebraExtensions.Random;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SaAmgSolverTests
	{
		[Fact]
		public static void TestForPoisson1D_12Elements()
		{
			CsrMatrix systemMatrix = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);
			Vector rhs = Vector.CreateFromArray(Poisson1D12ElementsSaAmgResults.RhsVector);

			SaAmgSolverBuilder solverBuilder = ChooseDefaultSolver();
			SaAmgSolver solver = solverBuilder.BuildSolver();
			Vector solution = Vector.CreateZero(rhs.Length);
			solver.Solve(systemMatrix, rhs, solution);

			// Expected output
			var solutionExpected = Vector.CreateFromArray(Poisson1D12ElementsSaAmgResults.Solution);
			double[] residualNormsExpected = Poisson1D12ElementsSaAmgResults.ResidualHistory;

			var expectedHierarchy = new SaAmgHierarchyLog();
			expectedHierarchy.NumLevels = 2;
			// Level 0
			expectedHierarchy.LogSystemMatrix(11, 11, 31);
			expectedHierarchy.LogStrengthOfConnection(11, 11, 31);
			expectedHierarchy.LogNumAggregates(4);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);
			expectedHierarchy.LogTentativeProlongation(11, 4, 11);
			expectedHierarchy.LogProlongation(11, 4, 17);
			expectedHierarchy.LogRestriction(4, 11, 17);
			// Level 1
			expectedHierarchy.LogSystemMatrix(4, 4, 10);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);

			// Comparisons
			AssertEqualHierarchyLogs(expectedHierarchy, solver.Logger.GetOperatorHierarchy(analysisStep: 0));
			var comparer = new MatrixComparer(tolerance: 1E-15);
			comparer.AssertEqual(solutionExpected, solution);
			ResidualNormHistory residualHistory = solver.Logger.GetResidualHistory(analysisStep: 0);
			AssertUtilities.AssertEqualResidualHistories(residualNormsExpected, residualHistory, tolerance: 1E-8);
		}

		private static SaAmgSolverBuilder ChooseDefaultSolver()
		{
			var solverBuilder = new SaAmgSolverBuilder(numLevelsRequested: 10, maxCycles: 1000);
			solverBuilder.ResidualTolerance = 1E-10;
			solverBuilder.CoarseningTermination = new SystemOrderBasedTermination(10);
			solverBuilder.StrengthOfConnection = new SymmetricStrengthOfConnection(theta: 0.0);
			solverBuilder.Aggregation = new StandardAggregation();
			solverBuilder.NullspaceCandidatesSelection = new ConstantCandidateSelection();
			solverBuilder.SetNullspaceCandidatesRelaxationAll(new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4));
			solverBuilder.NullspaceOrthonormalizationTolerance = 1E-10;
			IJacobiWeightingStrategy weighting = new GershgorinJacobiWeighting(dampingFactor: 4.0 / 3);
			solverBuilder.ProlongationSmoothing = new JacobiProlongationSmoothing(weighting, degree: 1);
			solverBuilder.Restriction = new SimpleTransposeRestriction();
			solverBuilder.SetSmoothers(new StationaryIterationSmoother(new SgsIterationCsr(), numSteps: 1));
			solverBuilder.CoarsestSystemSolver = new CholeskyCscCoarseSolver(solverBuilder.LinearAlgebraProvider, new AmdSymmetricOrdering());
			solverBuilder.CycleSchedule = new VCycleSchedule();
			solverBuilder.DiscardIntermediateHierarchyData = true;

			return solverBuilder;
		}

		private static void AssertEqualHierarchyLogs(SaAmgHierarchyLog expectedlog, SaAmgHierarchyLog computedLog)
		{
			Assert.Equal(expectedlog.NumLevels, computedLog.NumLevels);
			int maxLevel = expectedlog.NumLevels - 1;

			for (int lvl = 0; lvl < maxLevel; lvl++)
			{
				AssertEqualMatrixSizes(expectedlog.GetSystemMatrixSize(lvl), computedLog.GetSystemMatrixSize(lvl));
				AssertEqualMatrixSizes(expectedlog.GetProlongationSize(lvl), computedLog.GetProlongationSize(lvl));
				AssertEqualMatrixSizes(expectedlog.GetRestrictionSize(lvl), computedLog.GetRestrictionSize(lvl));

				AssertEqualMatrixSizes(expectedlog.GetStrengthOfConnectionSize(lvl), computedLog.GetStrengthOfConnectionSize(lvl));
				Assert.Equal(expectedlog.GetNumAggregates(lvl), computedLog.GetNumAggregates(lvl));
				Assert.Equal(expectedlog.GetNumNullspaceCandidateVectors(lvl), computedLog.GetNumNullspaceCandidateVectors(lvl));
				AssertEqualMatrixSizes(expectedlog.GetTentativeProlongationSize(lvl), computedLog.GetTentativeProlongationSize(lvl));
			}

			AssertEqualMatrixSizes(expectedlog.GetSystemMatrixSize(maxLevel), computedLog.GetSystemMatrixSize(maxLevel));
			Assert.Equal(expectedlog.GetNumNullspaceCandidateVectors(maxLevel), computedLog.GetNumNullspaceCandidateVectors(maxLevel));
		}

		private static void AssertEqualMatrixSizes(SparseMatrixSizeLog expected, SparseMatrixSizeLog computed)
		{
			Assert.Equal(expected.NumRows, computed.NumRows);
			Assert.Equal(expected.NumColumns, computed.NumColumns);
			Assert.Equal(expected.NumNonZeros, computed.NumNonZeros);
		}
	}
}
