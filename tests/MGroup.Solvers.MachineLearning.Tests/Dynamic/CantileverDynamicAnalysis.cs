namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{


	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Continuum;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Output.VTK;
	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.NumericalAnalyzers;
	using MGroup.NumericalAnalyzers.Dynamic;
	using MGroup.NumericalAnalyzers.Logging;
	using MGroup.Solvers.AlgebraicModel;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;

	public class CantileverDynamicAnalysis
	{
		public static void RunStochasticAnalysis()
		{
			//int numAnalysesTotal = 300;
			//int numSolutionsForTraining = 50;
			//int numPrincipalComponents = 8;

			//var solverFactory = new TempAiSolver.Factory(numSolutionsForTraining, numPrincipalComponents);
			//solverFactory.PcgConvergenceTolerance = 1E-6;
			//solverFactory.PcgMaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
			//AmgAISolver solver = solverFactory.BuildSolver();

			//int[] numElements = { 4, 20 };
			//var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			//example.Rng = new Random(Seed: 23);

			//string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
			//double timeStep = 0.05;
			//double totalDuration = 3;
		}

		public static void RunStandAloneAnalysis()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
			int[] numElements = { 4, 20 };
			double timeStep = 0.05;
			double totalDuration = 3;

			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			example.ElasticityModulusMean = 200E6;
			//example.ElasticityModulusMean = 15E6;
			(Model model, double[] parameters) = example.CreateFemModel();

			var solverFactory = new TempAiSolver.Factory()
			{
				DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(),
				new NullReordering()),
			};
			GlobalAlgebraicModel<SkylineMatrix> algebraicModel = solverFactory.BuildAlgebraicModel(model);
			var solver = solverFactory.BuildSolver(algebraicModel);
			var problem = new ProblemStructural(model, algebraicModel);

			var linearAnalyzer = new LinearAnalyzer(algebraicModel, solver, problem);
			var dynamicAnalyzerBuilder = new NewmarkDynamicAnalyzer.Builder(algebraicModel, problem, linearAnalyzer,
				timeStep, totalDuration, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var dynamicAnalyzer = dynamicAnalyzerBuilder.Build();

			int parameterSet = 0;
			solver.OnModelParameterUpdate(parameterSet);
			dynamicAnalyzer.Initialize();
			dynamicAnalyzer.Solve();

			// Plotting
			var plotter = new DisplacementFieldWriter(2, model, workDirectory);
			for (int t = 0; t < dynamicAnalyzer.Steps; t++)
			{
				IGlobalVector solution = solver.SavedSolutions.GetSolution(parameterSet, t);
				plotter.WriteResults(algebraicModel, solution);
			}
		}
	}
}
