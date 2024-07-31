#pragma warning disable CA1305 // Specify IFormatProvider
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
	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;
	using MGroup.Solvers.MachineLearning.PodAmg.Surrogates;
	using MGroup.Solvers.MachineLearning.StochasticExtensions;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.KarhunenLoeve;

	public class CantileverDynamicAnalysis
	{
		private const string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
		private const string pythonInterpreter = "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates\\venv\\Scripts\\python.exe";
		private const string trainScript = "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates\\src\\cae_ffnn_dynamic_t_as_param\\train.py";
		private const string predictScript = "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates\\src\\cae_ffnn_dynamic_t_as_param\\predict.py";

		private const int numTimeSteps = 60;
		private const double timeStepSize = 0.05;
		private const bool printMsgsToConsole = true;
		private const int rngSeed = 23;

		private const double beamLength = 10;
		private const double beamSectionHeight = 2.0;
		private const double beamSectionWidth = 1.0;
		private const double elasticityModulusMean = 200E6;
		private const double elasticityModulusStdDev = 10E6;
		private const bool useKarhunenLoeve = true;
		private const int numKarhunenLoeveTerms = 6;
		private const double correlationLength = 0.5 * beamLength;

		public static void RunStochasticAnalysis()
		{
			int numAnalysesTotal = 300;
			int numAnalysesForTraining = 50;
			int numPrincipalComponents = 1; // 1, 5, 10, 15, 20

			int[] numElements = { 4, 20 };
			//int[] numElements = { 16, 80 };
			//int[] numElements = { 32, 160 };
			double beamLength = 10.0;

			Random rng = new Random(rngSeed);
			IRandomField1D elasticityField = DefineElasticityField(numElements, rng);

			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1], elasticityField);
			example.SetTime(numTimeSteps * timeStepSize, numTimeSteps);
			example.BeamLength = beamLength;
			example.BeamSectionHeight = beamSectionHeight;
			example.BeamSectionWidth = beamSectionWidth;

			CaeFfnnArchitecture architecture = DescribeSurrogate(numElements);
			var surrogate = new CaeFfnnSurrogateDynamicPythonTF(architecture, workDirectory, pythonModelID: 43);
			surrogate.Float64 = false;
			surrogate.TensorFlowSeed = rngSeed;
			surrogate.Splitter.MinTestSetPercentage = 0.0; // Set it to something that encompasses all timesteps of the affected parameter realizations
			surrogate.Splitter.MinValidationSetPercentage = 0.0; // This stays 0
			surrogate.SetPythonCodePaths(pythonInterpreter, trainScript, predictScript);
			surrogate.UseBinaryIOFilesForArrays = false;
			surrogate.UseSolutionDifferenceFromPreviousStep = true;

			ISolutionPredictionStrategy solutionPrediction = surrogate;
			//ISolutionPredictionStrategy solutionPrediction = new NullSolutionPredictionStrategy();
			//ISolutionPredictionStrategy solutionPrediction = new SolutionOfPreviousTimestepAsPrediction();

			var solverFactory = new DynamicAmgAiSolver.Factory(numAnalysesForTraining, numPrincipalComponents, solutionPrediction);
			solverFactory.DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			solverFactory.PcgConvergenceTolerance = 1E-6;
			solverFactory.PcgMaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
			solverFactory.TrainingStrategy = new BulkSolutionsTrainingStrategy(timeStepSavePeriod: 5); // 1, 5, 10, 15, 20
			//solverFactory.TrainingStrategy = new SeparateTimeStepSolutionsTrainingStrategy(numTimeSteps);
			solverFactory.KeepOnlyNonZeroPrincipalComponents = true;
			DynamicAmgAiSolver solver = solverFactory.BuildSolver();

			double averageNumIterationsInitialPrecond = 0;
			double averageNumIterationsMLPrecond = 0;
			double averageDurationInitialPrecond = 0;
			double averageDurationMLPrecond = 0;
			long trainingDuration = 0;
			int numDofs = 0;
			var responses = new List<double>(numAnalysesTotal);
			for (int i = 0; i < numAnalysesTotal; i++)
			{
				PrintLine($"*************** Analysis {i+1}/{numAnalysesTotal} ***************");
				AnalysisResults results = RunSingleAnalysis(i, solver, example);
				responses.Add(results.MonitorDofRespose);

				numDofs = results.NumDofs;
				if (i < numAnalysesForTraining)
				{
					averageNumIterationsInitialPrecond += results.AveragePcgIterations;
					averageDurationInitialPrecond += results.PreconditionerCalculationDuration + results.PcgSolutionDuration;
				}
				else
				{
					averageNumIterationsMLPrecond += results.AveragePcgIterations;
					averageDurationMLPrecond += results.PreconditionerCalculationDuration + results.PcgSolutionDuration;
				}

				if (results.MLTrainingDuration > 0)
				{
					PrintLine("");
					PrintLine($"Training duration = {results.MLTrainingDuration} ms.");
					trainingDuration = results.MLTrainingDuration;
					PrintLine("");
				}

				var msg = new StringBuilder();
				msg.Append($"Dofs = {results.NumDofs}. Preconditioner = {results.PreconditionerName}. ");
				msg.Append($"Average number of PCG iterations per timestep = {Math.Round(results.AveragePcgIterations)}. ");
				msg.Append($"Preconditioner calculation duration = {results.PreconditionerCalculationDuration} ms. ");
				msg.Append($"PCG solution duration (sum of all timesteps) = {results.PcgSolutionDuration} ms. ");
				msg.Append($"Total solver duration (sum of all timesteps) = {results.PreconditionerCalculationDuration + results.PcgSolutionDuration} ms. ");
				PrintLine(msg.ToString());
			}

			double mean = responses.Average();
			averageNumIterationsInitialPrecond /= numAnalysesForTraining;
			averageNumIterationsMLPrecond /= numAnalysesTotal - numAnalysesForTraining;
			averageDurationInitialPrecond /= numAnalysesForTraining;
			averageDurationMLPrecond /= numAnalysesTotal - numAnalysesForTraining;

			PrintLine($"Total analyses: {numAnalysesTotal}. Training analyses: {numAnalysesForTraining}. " +
				$"Mean uTop={mean}");
			PrintLine($"Num dofs = {numDofs}. \n 1) Initial preconditioner: " +
				$"Average PCG iterations per solution = {Math.Round(averageNumIterationsInitialPrecond)}. " +
				$"Average solution duration per analysis = {Math.Round(averageDurationInitialPrecond)} ms. " +
				$"\n 2) POD-2G preconditioner: " +
				$"Average PCG iterations per solution = {Math.Round(averageNumIterationsMLPrecond)}. " +
				$"Average solution duration per analysis = {Math.Round(averageDurationMLPrecond)} ms. " +
				$"Training duration = {trainingDuration} ms.");
		}

		private static AnalysisResults RunSingleAnalysis(
			int analysisNo, DynamicAmgAiSolver solver, CantileverDynamicModel example)
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
			for (int t = 0; t < numTimeSteps; t++)
			{
				numPcgIterations += solver.Logger.GetNumIterationsOfIterativeAlgorithm(t);
			}

			solver.Logger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.UpdatePreconditioner.ToString(), out long createDuration);
			solver.Logger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.SolveWithPcg.ToString(), out long solveDuration);
			solver.Logger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.TrainML.ToString(), out long trainingDuration);

			var results = new AnalysisResults()
			{
				MonitorDofRespose = response,
				NumDofs = solver.LinearSystem.Solution.SingleVector.Length,
				PreconditionerName = solver.CurrentPreconditionerName,
				AveragePcgIterations = ((double)numPcgIterations) / numTimeSteps,
				PreconditionerCalculationDuration = createDuration,
				PcgSolutionDuration = solveDuration,
				MLTrainingDuration = trainingDuration
			};

			return results;
		}

		public static void RunStandAloneAnalysis()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
			int[] numElements = { 32, 160 };
			bool useIterativeSolver = false;

			Random rng = new Random(rngSeed);
			IRandomField1D elasticityField = DefineElasticityField(numElements, rng);

			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1], elasticityField);
			example.BeamLength = beamLength;
			example.BeamSectionHeight = beamSectionHeight;
			example.BeamSectionWidth = beamSectionWidth;
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

		private static IRandomField1D DefineElasticityField(int[] numElementsPerAxis, Random rng)
		{
			if (useKarhunenLoeve)
			{
				int numNodesAlongBeamLength = numElementsPerAxis[numElementsPerAxis.Length - 1] + 1;

				var elasticityField = new KarhunenLoeveField1D(0, beamLength, numNodesAlongBeamLength,
					elasticityModulusMean, elasticityModulusStdDev, correlationLength, numKarhunenLoeveTerms, rng);
				return elasticityField;
			}
			else
			{
				int numElementsTotal = 1;
				for (int d = 0; d < numElementsPerAxis.Length; d++)
				{
					numElementsTotal *= numElementsPerAxis[d];
				}

				var elasticityField = new WhiteNoiseField(elasticityModulusMean, elasticityModulusStdDev,
					numElementsTotal, rng, true);
				return elasticityField;
			}
		}

		private static CaeFfnnArchitecture DescribeSurrogate(int[] numElementsPerAxis)
		{
			if ((numElementsPerAxis[0] == 4) && (numElementsPerAxis[1] == 20)) //200 dofs
			{
				int ffnnHiddenSize = 32;

				var arch = new CaeFfnnArchitecture();

				arch.NumDofs = 2 * (numElementsPerAxis[0] + 1) * numElementsPerAxis[1]; //200
				if (useKarhunenLoeve)
				{
					arch.NumModelParams = numKarhunenLoeveTerms + 1;
				}
				else
				{
					arch.NumModelParams = numElementsPerAxis[0] * numElementsPerAxis[1] + 1; //80 element E + 1 time
				}
				arch.LatentSpaceDim = 8;

				arch.CaeLearningRate = 5E-4f;
				arch.CaeNumEpochs = 40;
				arch.CaeBatchSize = 10;
				arch.FfnnLearningRate = 1E-4f;
				arch.FfnnNumEpochs = 500; //3000 took too long for 81 model params
				arch.FfnnBatchSize = 20;

				arch.EncoderLayers.Add(new Conv1DLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
				arch.EncoderLayers.Add(new LeakyReLULayer());
				arch.EncoderLayers.Add(new Conv1DLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
				arch.EncoderLayers.Add(new LeakyReLULayer());
				arch.EncoderLayers.Add(new Conv1DLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
				arch.EncoderLayers.Add(new LeakyReLULayer());
				arch.EncoderLayers.Add(new Conv1DLayer(filters: 16, kernelSize: 5, strides: 1, padding: "same"));
				arch.EncoderLayers.Add(new LeakyReLULayer());
				arch.EncoderLayers.Add(new FlattenLayer());
				arch.EncoderLayers.Add(new DenseLayer(units: arch.LatentSpaceDim));

				arch.DecoderLayers.Add(new Input1DLayer(arch.LatentSpaceDim));
				arch.DecoderLayers.Add(new DenseLayer(units: 32));
				arch.DecoderLayers.Add(new LeakyReLULayer());
				arch.DecoderLayers.Add(new ReshapeLayer(new int[] { 1, 32 }));
				arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 32, kernelSize: 5, strides: 1, padding: "same"));
				arch.DecoderLayers.Add(new LeakyReLULayer());
				arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 64, kernelSize: 5, strides: 1, padding: "same"));
				arch.DecoderLayers.Add(new LeakyReLULayer());
				arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: 128, kernelSize: 5, strides: 1, padding: "same"));
				arch.DecoderLayers.Add(new LeakyReLULayer());
				arch.DecoderLayers.Add(new Conv1DTransposeLayer(filters: arch.NumDofs, kernelSize: 5, strides: 1, padding: "same"));

				arch.FfnnLayers.Add(new Input1DLayer(arch.NumModelParams));
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: ffnnHiddenSize));
				arch.FfnnLayers.Add(new LeakyReLULayer());
				arch.FfnnLayers.Add(new DenseLayer(units: arch.LatentSpaceDim));

				return arch;
			}
			else
			{
				throw new NotImplementedException();
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
			public long PcgSolutionDuration { get; set; }

			public long MLTrainingDuration { get; set; }
		}
	}
}
#pragma warning restore CA1305 // Specify IFormatProvider
