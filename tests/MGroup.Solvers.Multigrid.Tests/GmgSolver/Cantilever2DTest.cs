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
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests;

	using Xunit;

	public static class Cantilever2DTest
	{
		[Fact]
		private static void RunTest()
		{
			// Model
			int[] numElements = [2, 2];
			var example = new CantileverQuad4ElasticExample();
			example.LengthX = 2;
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
			var linearAlgebraProvider = new ManagedSequentialImplementationProvider();
			var solverBuilder = new GmgSolverBuilder(numLevels: 2, grid, prolongation, maxCycles: 100, linearAlgebraProvider);
			solverBuilder.CycleSchedule = new VCycleSchedule();
			var smoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(), 2);
			//var smoother = new StationaryIterationSmoother(new JacobiIterationCsr(), 2);
			solverBuilder.SetSmoothers(smoother);
			solverBuilder.CoarsestSystemSolver = new CholeskyCscCoarseSolver(linearAlgebraProvider, new AmdSymmetricOrdering());
			solverBuilder.ResidualTolerance = 1E-8;
			solverBuilder.DropToleranceForSmallEntriesOfSystemMatrix = 1E-10;

			// Run analysis
			GlobalAlgebraicModel<CsrMatrix> algebraicModel = solverBuilder.BuildAlgebraicModel(model);
			MultigridSolver solver = solverBuilder.BuildSolver(model, dofsPerNode, algebraicModel);
			IVector solution = RunAnalysis(model, algebraicModel, solver);

			// Reference solution
			Model refModel = example.CreateFemModel(numElements[0], numElements[1]);
			(IAlgebraicModel refAlgModel, ISolver refSolver) = SetupReferenceSolver(refModel);
			IVector refSolution = RunAnalysis(refModel, refAlgModel, refSolver);

			// Check results
			var comparer = new MatrixComparer();
			comparer.AssertEqual(refSolution, solution);
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
