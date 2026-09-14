namespace MGroup.Solvers.Multigrid.Tests.GmgSolver
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.Iterative;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Tests;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.TestOptions;

	using Xunit;

	public static class Cantilever2DTest
	{
		[Theory]
		[InlineData(4, Cycles.V, Smoothers.GS, 1.0, 2, 13)]
		[InlineData(4, Cycles.V, Smoothers.GS, 1.0, 3, 11)]
		[InlineData(4, Cycles.V, Smoothers.SGS, 1.0, 1, 15)]
		[InlineData(4, Cycles.V, Smoothers.SGS, 1.0, 2, 11)]
		[InlineData(4, Cycles.V, Smoothers.SOR, 1.1, 2, 12)]
		[InlineData(4, Cycles.V, Smoothers.SSOR, 1.1, 2, 11)]
		[InlineData(4, Cycles.W, Smoothers.GS, 1.0, 2, 10)]
		[InlineData(4, Cycles.F, Smoothers.GS, 1.0, 2, 10)]
		[InlineData(3, Cycles.V, Smoothers.GS, 1.0, 2, 12)]
		[InlineData(2, Cycles.V, Smoothers.GS, 1.0, 2, 10)]
		private static void RunTest(int numLevels, Cycles cycle, Smoothers smoother, double relaxFactor, int smoothingSteps, int numCyclesExpected)
		{
			// Model
			int[] numElements = [32, 160];
			var example = new CantileverQuad4ElasticExample();
			example.LengthX = 0.4;
			example.LengthY = 2;
			example.Thickness = 0.1;
			example.YoungModulus = 200E6;
			example.PoissonRatio = 0.3;
			example.EndPointLoad = 1000;
			example.ParallelToX = false;
			Model model = example.CreateFemModel(numElements[0], numElements[1]);
			IDofType[] dofsPerNode = [StructuralDof.TranslationX, StructuralDof.TranslationY];

			// Setup solver
			var grid = new Grid2D(numElements[0] + 1, numElements[1] + 1);
			var prolongation = new Prolongation2DVectorStrategy();
			var solverBuilder = new GmgSolverBuilder(numLevels: numLevels, grid, prolongation, maxCycles: 100);
			solverBuilder.LinearAlgebraProvider = new ManagedSequentialImplementationProvider();
			solverBuilder.CycleSchedule = cycle.Translate();
			IStationaryIteration stationaryIteration = smoother.Translate(relaxFactor);
			solverBuilder.SetSmoothers(new StationaryIterationSmoother(stationaryIteration, smoothingSteps));
			solverBuilder.CoarsestSystemSolver = new CholeskyCscCoarseSolver(solverBuilder.LinearAlgebraProvider, new AmdSymmetricOrdering());
			solverBuilder.ResidualTolerance = 1E-10;
			solverBuilder.DropToleranceForSmallEntriesOfSystemMatrix = 1E-10;

			// Run analysis
			GlobalAlgebraicModel<CsrMatrix> algebraicModel = solverBuilder.BuildAlgebraicModel(model);
			MultigridSolver solver = solverBuilder.BuildSolver(model, dofsPerNode, algebraicModel);
			IVector solution = RunAnalysis(model, algebraicModel, solver);

			// Reference solution
			Model refModel = example.CreateFemModel(numElements[0], numElements[1]);
			(IAlgebraicModel refAlgModel, ISolver refSolver) = SetupReferenceSolver(refModel);
			IVector refSolution = RunAnalysis(refModel, refAlgModel, refSolver);

			// Check solution
			double solutionTol = 1E-10;
			double normRelative = refSolution.Subtract(solution).Norm2() / refSolution.Norm2();
			Assert.InRange(normRelative, 0, solutionTol);

			// Check convergence
			double resTolExpected = solverBuilder.ResidualTolerance;
			int analysisStep = 0;
			int numCycles = solver.Logger.GetNumIterationsOfIterativeAlgorithm(analysisStep);
			double resTol = solver.Logger.GetResidualNormRatioOfIterativeAlgorithm(analysisStep);
			Assert.InRange(numCycles, 0, numCyclesExpected);
			Assert.InRange(resTol, 0, resTolExpected);
		}

		private static IVector RunAnalysis(Model model, IAlgebraicModel algebraicModel, ISolver solver)
		{
			// Structural problem provider
			var problem = new ProblemStructural(model, algebraicModel);

			// Linear static analysis
			var childAnalyzer = new LinearAnalyzer(algebraicModel, solver, problem);
			var parentAnalyzer = new StaticAnalyzer(algebraicModel, problem, childAnalyzer);

			// Run the analysis
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			return algebraicModel.LinearSystem.Solution;
		}

		private static (IAlgebraicModel refAlgModel, ISolver refSolver) SetupReferenceSolver(Model model)
		{
			var solverBuilder = new LdlSkylineSolver.Factory();
			GlobalAlgebraicModel<SkylineMatrix> algebraicModel = solverBuilder.BuildAlgebraicModel(model);
			ISolver solver = solverBuilder.BuildSolver(algebraicModel);
			return (algebraicModel, solver);
		}
	}
}
