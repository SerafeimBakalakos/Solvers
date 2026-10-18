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
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class SaAmgSolverTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			CsrMatrix systemMatrix = Poisson1D12ElementsSaAmgResults.GetSystemMatrix(level: 0);
			Vector rhs = Vector.CreateFromArray(Poisson1D12ElementsSaAmgResults.RhsVector);
			Vector solutionExpected = Vector.CreateFromArray(Poisson1D12ElementsSaAmgResults.Solution);
			double[] residualNormsExpected = Poisson1D12ElementsSaAmgResults.ResidualHistory;

			var solverBuilder = new SaAmgSolverBuilder(numLevelsRequested: 10, maxCycles: 100);
			solverBuilder.ResidualTolerance = 1E-10;
			solverBuilder.CoarseningTermination = new SystemOrderBasedTermination(10);
			solverBuilder.StrengthOfConnection = new SymmetricStrengthOfConnection(theta: 0.0);
			solverBuilder.Aggregation = new StandardAggregation();
			solverBuilder.NullspaceCandidatesSelection = new ConstantCandidateSelection();
			solverBuilder.SetNullspaceCandidatesRelaxationAll(new StationaryIterationCandidatesRelaxation(new SgsIterationCsr(), numSteps: 4));
			solverBuilder.NullspaceOrthonormalizationTolerance = 1E-10;
			solverBuilder.ProlongationSmoothing = new JacobiProlongationSmoothing(weightFactor: 4.0 /3, degree: 1);
			solverBuilder.Restriction = new SimpleTransposeRestriction();
			solverBuilder.SetSmoothers(new StationaryIterationSmoother(new SgsIterationCsr(), numSteps: 1));
			solverBuilder.CoarsestSystemSolver = new CholeskyCscCoarseSolver(solverBuilder.LinearAlgebraProvider, new AmdSymmetricOrdering());
			solverBuilder.CycleSchedule = new VCycleSchedule();
			solverBuilder.DiscardIntermediateHierarchyData = true;

			SaAmgSolver solver = solverBuilder.BuildSolver();
			Vector solution = Vector.CreateZero(rhs.Length);
			solver.Solve(systemMatrix, rhs, solution);

			var comparer = new MatrixComparer(tolerance: 1E-30);
			comparer.AssertEqual(solutionExpected, solution);
			ResidualNormHistory residualHistory = solver.Logger.GetResidualHistory(analysisStep: 0);
			AssertUtilities.AssertEqualResidualHistories(residualNormsExpected, residualHistory, tolerance: 1E-10);
		}
	}
}
