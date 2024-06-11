namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.MachineLearning.AnalyzersExtensions;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;

	public class CantileverDynamicAnalysis
	{
		//private const string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
		private const int numTimeSteps = 60;
		private const double timeStepSize = 0.05;
		private const bool printMsgsToConsole = true;

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

			//int[] numElements = { 4, 20 };
			int[] numElements = { 16, 80 };
			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			example.SetTime(numTimeSteps * timeStepSize, numTimeSteps);
			example.Rng = new Random(Seed: 23);

			var responses = new List<double>(numAnalysesTotal);
			for (int i = 0; i < numAnalysesTotal; i++)
			{
				AnalysisResults results = RunSingleAnalysis(i, solver, example);
				responses.Add(results.MonitorDofRespose);

				PrintLine($"*************** Analysis {i+1}/{numAnalysesTotal} ***************");
				var msg = new StringBuilder();
				msg.Append($"Dofs = {results.NumDofs}. Preconditioner = {results.PreconditionerName}. ");
				msg.Append($"Average number of PCG iterations per timestep = {Math.Round(results.AveragePcgIterations)}. ");
				msg.Append($"Preconditioner calculation duration = {results.PreconditionerCalculationDuration} ms. ");
				msg.Append($"PCG solution duration (sum of all timesteps) = {results.TotalPcgDuration} ms.");
				PrintLine(msg.ToString());
			}

			double mean = responses.Average();
			PrintLine($"Total analyses: {numAnalysesTotal}. Training analyses: {numAnalysesForTraining}. " +
				$"Mean uTop={mean}");
		}

		private static AnalysisResults RunSingleAnalysis(
			int analysisNo, AmgAiSolver2 solver, CantileverDynamicModel example)
		{
			(Model model, double[] parameters, int monitorNodeId) = example.CreateFemModel();
			INode monitorNode = model.GetNode(monitorNodeId);

			solver.SetModel(analysisNo, parameters, model);
			var problem = new ProblemStructural(model, solver.AlgebraicModel);

			var linearAnalyzer = new LinearAnalyzer(solver.AlgebraicModel, solver, problem);
			var dynamicAnalyzerBuilder = new NewmarkDynamicAnalyzer.Builder(solver.AlgebraicModel, problem, linearAnalyzer,
				timeStepSize, timeStepSize * numTimeSteps, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var dynamicAnalyzer = dynamicAnalyzerBuilder.Build();

			dynamicAnalyzer.Initialize();
			dynamicAnalyzer.Solve();

			double response = solver.AlgebraicModel.ExtractSingleValue(
				solver.LinearSystem.Solution, monitorNode, StructuralDof.TranslationX);

			int numPcgIterations = 0;
			long duration = 0;
			for (int t = 0; t < numTimeSteps; t++)
			{
				numPcgIterations += solver.Logger.GetNumIterationsOfIterativeAlgorithm(t);
			}

			solver.Logger.TryGetTaskDuration(AmgAiSolver2.Subtask.CreatePreconditioner.ToString(), out long createDuration);
			solver.Logger.TryGetTaskDuration(AmgAiSolver2.Subtask.SolveWithPcg.ToString(), out long solveDuration);

			var results = new AnalysisResults()
			{
				MonitorDofRespose = response,
				NumDofs = solver.LinearSystem.Solution.SingleVector.Length,
				PreconditionerName = solver.CurrentPreconditionerName,
				AveragePcgIterations = ((double)numPcgIterations) / numTimeSteps,
				PreconditionerCalculationDuration = createDuration,
				TotalPcgDuration = solveDuration,
			};

			return results;
		}

		public static void RunStandAloneAnalysis()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
			int[] numElements = { 4, 20 };
			bool useIterativeSolver = true;

			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1]);
			example.ElasticityModulusMean = 200E6;
			//example.ElasticityModulusMean = 15E6;
			example.SetTime(timeStepSize * numTimeSteps, numTimeSteps);
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
				timeStepSize, timeStepSize * numTimeSteps, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var analyzer = dynamicAnalyzerBuilder.Build();
			//var analyzer = new StaticAnalyzer(solver.Model, problem, linearAnalyzer);

			int parameterSet = 0;
			solver.OnModelParameterUpdate(parameterSet);
			analyzer.Initialize();
			analyzer.Solve();

			// Plotting
			var plotter = new DisplacementFieldWriter(2, model, workDirectory);
			for (int t = 0; t < solver.SavedSolutions.NumTimeSteps; t++)
			{
				IGlobalVector solution = solver.SavedSolutions.GetSolution(parameterSet, t);
				plotter.WriteResults(solver.Model, solution);
			}
		}

		private static void PrintLine(string msg)
		{
			if (printMsgsToConsole)
			{
				Console.WriteLine(msg);
			}
			else
			{
				Debug.WriteLine(msg);
			}
		}

		private class AnalysisResults
		{
			public double MonitorDofRespose { get; set; }

			public int NumDofs { get; set; }

			public double AveragePcgIterations { get; set; }

			public string PreconditionerName { get; set; }

			/// <summary>
			/// In milliseconds
			/// </summary>
			public long PreconditionerCalculationDuration { get; set; }

			/// <summary>
			/// In milliseconds
			/// </summary>
			public double TotalPcgDuration { get; set; }
		}
	}
}
