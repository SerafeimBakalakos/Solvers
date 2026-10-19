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

		[Fact]
		public static void TestForPoisson1D_100Elements()
		{
			(DokRowMajor dokA, Vector rhs) = Poisson1DProblem.CreateWithConstantSource(100, source: 1.0);
			CsrMatrix systemMatrix = dokA.BuildCsrMatrix(sortColsOfEachRow: true);

			SaAmgSolverBuilder solverBuilder = ChooseDefaultSolver();
			SaAmgSolver solver = solverBuilder.BuildSolver();
			Vector solution = Vector.CreateZero(rhs.Length);
			solver.Solve(systemMatrix, rhs, solution);

			// Expected output
			var solutionExpected = Vector.CreateFromArray(new double[]
			{
				0.004949999999999166, 0.0097999999999983326, 0.01454999999999813, 0.019199999999999041, 0.023750000000000934, 0.028200000000000381, 0.03254999999999688, 0.03679999999999288, .040949999999992659, 0.044999999999996029, 0.048949999999997579, 0.052800000000001332, 0.056550000000007906, 0.060200000000015935, 0.063750000000016641, 0.067200000000010141, 0.070550000000005775, 0.0738000000000021, 0.076949999999998506, 0.079999999999993798, 0.082949999999993029, 0.08579999999999624, 0.088549999999998588, 0.09119999999999924, 0.093749999999998709, 0.096199999999999494, 0.098549999999997528, 0.10079999999999259, 0.10294999999998809, 0.10499999999998469, 0.10694999999998198, .10879999999997744, 0.11054999999997793, 0.1121999999999837, 0.11374999999998871, 0.11519999999999135, 0.11654999999999195, 0.11779999999999385, .11894999999999337, 0.11999999999999013, 0.12094999999998633, 0.12179999999998671, 0.12254999999999132, 0.1231999999999954, 0.12374999999999682, 0.12419999999999602, 0.12454999999999677, 0.12479999999999547, 0.12494999999999162, 0.12499999999998712, 0.12494999999998703, 0.12479999999999138, 0.12454999999999523, 0.1241999999999959, 0.12374999999999377, 0.1231999999999932, 0.12254999999999057, 0.12179999999998538, 0.12094999999997924, 0.11999999999997885, 0.11894999999998414, 0.11779999999998828, 0.11654999999999033, 0.11519999999999073, 0.11374999999999234, 0.11219999999999108, 0.11054999999998677, 0.10879999999998217, 0.10694999999998193, 0.10499999999998573, 0.10294999999998758, 0.10079999999999169, 0.098549999999998528, 0.096200000000005698, 0.0937500000000102, 0.091200000000012216, 0.088550000000016477, 0.085800000000015475, 0.082950000000009017, 0.080000000000004082, 0.076949999999998756, 0.07379999999999276, 0.070549999999986235, 0.06719999999998226, 0.063749999999980794, 0.060199999999978049, 0.056549999999977507, 0.052799999999978933, 0.048949999999978192, 0.044999999999982852, 0.040949999999993034, 0.03680000000000308, 0.03255000000000554, 0.028200000000000561, 0.023749999999995747, 0.019199999999997823, 0.014550000000007026, 0.0098000000000172758, 0.0049500000000150205
			});
			double[] residualNormsExpected = [0.099498743710661988, 0.040596605531467739, 0.0020390065516121617, 0.00017745330227311371, 1.8002184928153758e-05, 1.901814681701041e-06, 2.0556933757646443e-07, 2.2585025125036977e-08, 2.5102745076298793e-09, 2.8130031032114691e-10, 3.1705670465900162e-11, 3.5899455521310058e-12];

			var expectedHierarchy = new SaAmgHierarchyLog();
			expectedHierarchy.NumLevels = 4;

			// Level 0
			expectedHierarchy.LogSystemMatrix(99, 99, 295);
			expectedHierarchy.LogStrengthOfConnection(99, 99, 295);
			expectedHierarchy.LogNumAggregates(33);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);
			expectedHierarchy.LogTentativeProlongation(99, 33, 99);
			expectedHierarchy.LogProlongation(99, 33, 163);
			expectedHierarchy.LogRestriction(33, 99, 163);

			// Level 1
			expectedHierarchy.LogSystemMatrix(33, 33, 97);
			expectedHierarchy.LogStrengthOfConnection(33, 33, 97);
			expectedHierarchy.LogNumAggregates(11);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);
			expectedHierarchy.LogTentativeProlongation(33, 11, 33);
			expectedHierarchy.LogProlongation(33, 11, 53);
			expectedHierarchy.LogRestriction(11, 33, 53);

			// Level 2
			expectedHierarchy.LogSystemMatrix(11, 11, 31);
			expectedHierarchy.LogStrengthOfConnection(11, 11, 31);
			expectedHierarchy.LogNumAggregates(4);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);
			expectedHierarchy.LogTentativeProlongation(11, 4, 11);
			expectedHierarchy.LogProlongation(11, 4, 17);
			expectedHierarchy.LogRestriction(4, 11, 17);

			// Level 3 (coarsest)
			expectedHierarchy.LogSystemMatrix(4, 4, 10);
			expectedHierarchy.LogNumNullspaceCandidateVectors(1);

			// Comparisons
			AssertEqualHierarchyLogs(expectedHierarchy, solver.Logger.GetOperatorHierarchy(analysisStep: 0));
			var comparer = new MatrixComparer(tolerance: 1E-14);
			comparer.AssertEqual(solutionExpected, solution);
			ResidualNormHistory residualHistory = solver.Logger.GetResidualHistory(analysisStep: 0);
			AssertUtilities.AssertEqualResidualHistories(residualNormsExpected, residualHistory, tolerance: 1E-6);
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
