namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{


	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
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
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Output.VTK;
	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.NumericalAnalyzers;
	using MGroup.NumericalAnalyzers.Logging;
	using MGroup.Solvers.AlgebraicModel;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.MachineLearning.AnalyzersExtensions;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;

	public class CantileverDynamicAnalysis
	{
		//private const string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";

		public static void RunStochasticAnalysis()
		{
			int numAnalysesTotal = 300;
			int numAnalysesForTraining = 50;
			int numPrincipalComponents = 8;

			var solverFactory = new AmgAiSolver2.Factory(numAnalysesForTraining, numPrincipalComponents);
			solverFactory.DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			solverFactory.PcgConvergenceTolerance = 1E-6;
			solverFactory.PcgMaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
			AmgAiSolver2 solver = solverFactory.BuildSolver();

			int[] numElements = { 4, 20 };
			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			example.Rng = new Random(Seed: 23);

			var responses = new List<double>(numAnalysesTotal);
			for (int i = 1; i <= numAnalysesTotal; i++)
			{
				(double monitoredResponse, int numDofs, int numPcgIterations) = RunSingleAnalysis(i, solver, example);
				responses.Add(monitoredResponse);

				Debug.WriteLine($"*************** Analysis {i}/{numAnalysesTotal} ***************");
				Debug.WriteLine($"Number of PCG iterations = {numPcgIterations}. Dofs = {numDofs}.");
			}

			double mean = responses.Average();
			Debug.WriteLine($"Total analyses: {numAnalysesTotal}. Training analyses: {numAnalysesForTraining}. " +
				$"Mean uTop={mean}");
		}

		private static (double monitoredResponse, int numDofs, int numPcgIterations) RunSingleAnalysis(
			int analysisNo, AmgAiSolver2 solver, CantileverDynamicModel example)
		{
			double timeStep = 0.05;
			double totalDuration = 3;

			(Model model, double[] parameters, int monitorNodeId) = example.CreateFemModel();
			INode monitorNode = model.GetNode(monitorNodeId);

			solver.SetModel(analysisNo, parameters, model);
			var problem = new ProblemStructural(model, solver.AlgebraicModel);

			var linearAnalyzer = new LinearAnalyzer(solver.AlgebraicModel, solver, problem);
			var dynamicAnalyzerBuilder = new NewmarkDynamicAnalyzer.Builder(solver.AlgebraicModel, problem, linearAnalyzer,
				timeStep, totalDuration, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var dynamicAnalyzer = dynamicAnalyzerBuilder.Build();

			dynamicAnalyzer.Initialize();
			dynamicAnalyzer.Solve();

			double computedDisplacement = solver.AlgebraicModel.ExtractSingleValue(
				solver.LinearSystem.Solution, monitorNode, StructuralDof.TranslationZ);
			int numPcgIterations = solver.Logger.GetNumIterationsOfIterativeAlgorithm(analysisNo - 1);
			return (computedDisplacement, solver.LinearSystem.Solution.SingleVector.Length, numPcgIterations);
		}

		public static void RunStandAloneAnalysis()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
			int[] numElements = { 4, 20 };
			double timeStep = 0.05;
			int numSteps = 60;
			double totalDuration = timeStep * numSteps;
			bool useIterativeSolver = false;

			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			example.ElasticityModulusMean = 200E6;
			//example.ElasticityModulusMean = 15E6;
			example.SetTime(totalDuration, numSteps);
			(Model model, _, _) = example.CreateFemModel();

			ITempSolver solver;
			var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			if (useIterativeSolver)
			{
				var solverFactory = new TempSolverIterative.Factory()
				{
					DofOrderer = dofOrderer,
				};
				solver = solverFactory.BuildSolver(solverFactory.BuildAlgebraicModel(model));
			}
			else
			{
				var solverFactory = new TempSolverDirect.Factory()
				{
					DofOrderer = dofOrderer,
				};
				solver = solverFactory.BuildSolver(solverFactory.BuildAlgebraicModel(model));
			}

			var problem = new ProblemStructural(model, solver.Model);

			var linearAnalyzer = new LinearAnalyzer(solver.Model, solver, problem);
			var dynamicAnalyzerBuilder = new NewmarkDynamicAnalyzer.Builder(solver.Model, problem, linearAnalyzer,
				timeStep, totalDuration, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var analyzer = dynamicAnalyzerBuilder.Build();
			//var analyzer = new StaticAnalyzer(solver.Model, problem, linearAnalyzer);

			int parameterSet = 0;
			solver.OnModelParameterUpdate(parameterSet);
			analyzer.Initialize();
			analyzer.Solve();

			// Plotting
			var plotter = new DisplacementFieldWriter(2, model, workDirectory);
			for (int t = 0; t < analyzer.Steps; t++)
			{
				IGlobalVector solution = solver.SavedSolutions.GetSolution(parameterSet, t);
				plotter.WriteResults(solver.Model, solution);
			}
		}
	}
}
