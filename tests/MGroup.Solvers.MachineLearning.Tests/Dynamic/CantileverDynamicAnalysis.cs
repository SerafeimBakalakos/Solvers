#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable SA1516 // Elements should be separated by blank line
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
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.AlgebraicModel;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.MachineLearning.AnalyzersExtensions;
	using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.IterativeMethods.PCG;
	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;
	using MGroup.Solvers.MachineLearning.PodAmg.Surrogates;
	using MGroup.Solvers.MachineLearning.StochasticExtensions;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.KarhunenLoeve;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.RandomNumberGeneration;
	using MGroup.Solvers.MachineLearning.Tests.StochasticExtensions;

	public class CantileverDynamicAnalysis : IAutoStochasticAnalysis
	{
		// Paths
		private const bool runOnCluster = false;
		private const string workDirectory = runOnCluster ?
			"C:\\Users\\cluster\\Desktop\\AISolve\\results\\CantileverDynamicLinear"
			: "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
		private const string pythonProjectDirectory = runOnCluster ?
			"C:\\Users\\cluster\\Desktop\\AISolve\\code\\Python\\cs2py_ml_surrogates"
			: "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates";
		private const string pythonInterpreter = pythonProjectDirectory + "\\venv\\Scripts\\python.exe";
		private const string trainScript = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\train.py";
		private const string predictScript = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\predict.py";

		// Number of analyses
		private const int numAnalysesTotal = 300;
		private const int numAnalysesForTraining = 50;
		private const int numTimeSteps = 60;
		private const double timeStepSize = 0.05;

		// Model: geometry
		private static readonly int[] numElements = { 4, 20 };
		//private static readonly int[] numElements = { 16, 80 };
		//private static readonly int[] numElements = { 32, 160 };
		private const double beamLength = 10;
		private const double beamSectionHeight = 2.0;
		private const double beamSectionWidth = 1.0;

		// Model: material
		private const double elasticityModulusMean = 200E6;
		private const double elasticityModulusStdDev = 10E6;
		private const bool useKarhunenLoeve = true;
		private const int numKarhunenLoeveTerms = 6;
		private const double correlationLength = 0.5 * beamLength;

		// Solver: general
		private const double pcgTol = 1E-6;
		private const bool pcgConvergenceBasedOnResidualOnly = true;
		private const bool useDirectSolverInstead = false;

		// Solver: POD
		private const int numPrincipalComponents = 10; // 1 (not that effective), 5, 10 (start here), 15, 20 (doubtful)
		private const int timeStepSavePeriod = 5; // 1 (too expensive), 5 (good), 10 (good), 15, 20
		private const bool useAlwaysInitialPreconditioner = false;

		// Solver: surrogate
		private const bool enableSurrogate = true;
		private const bool useSolutionFromPreviousStep = true;
		private const bool useBinaryIOFiles = true;

		// Reports
		private const bool printAnalysisMessagesToConsole = true;
		private const bool printSurrogatePredictionMessagesToConsole = false;

		// Misc
		private const char saveLoadOrNotPretrainingAnalyses = 'N'; // 'S' for save, 'L' for load, anything else for neither.
		private const int rngSeed = 23;

		private static CaeFfnnArchitecture DescribeSurrogate(int[] numElementsPerAxis)
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

		public static void RunStochasticAnalysis()
		{
			var analysis = new CantileverDynamicAnalysis();
			StochasticAnalysisRunner runner = analysis.PrepareStochasticAnalysis(numAnalysesTotal, numAnalysesForTraining);
			runner.RunAll();
		}

		private CantileverDynamicModel example;
		private DynamicAmgAiSolver solver;
		private SkylineSolver.Factory directSolverFactory;

		public CantileverDynamicAnalysis()
		{
		}

		public void InitializeModel(RepeatableRandom rng)
		{
			IRandomField1D elasticityField = DefineElasticityField(numElements, rng);
			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1], elasticityField);
			example.SetTimeSteps(numTimeSteps * timeStepSize, numTimeSteps);
			example.BeamLength = beamLength;
			example.BeamSectionHeight = beamSectionHeight;
			example.BeamSectionWidth = beamSectionWidth;

			this.example = example;
		}

		public void InitializeSolver()
		{
			if (useDirectSolverInstead)
			{
				directSolverFactory = new SkylineSolver.Factory();
				return;
			}

			CaeFfnnArchitecture architecture = DescribeSurrogate(numElements);
			var surrogate = new CaeFfnnSurrogateDynamicPythonTF(architecture, workDirectory, pythonModelID: 43);
			//surrogate.float64 = false;
			surrogate.TensorFlowSeed = rngSeed;
			surrogate.Splitter.MinTestSetPercentage = 0.0; // Set it to something that encompasses all timesteps of the affected parameter realizations
			surrogate.Splitter.MinValidationSetPercentage = 0.0; // This stays 0
			surrogate.SetPythonCodePaths(pythonInterpreter, trainScript, predictScript);
			surrogate.UseBinaryIOFilesForArrays = useBinaryIOFiles;
			surrogate.UseSolutionDifferenceFromPreviousStep = useSolutionFromPreviousStep;
			surrogate.WriteTrainReportToConsole = printAnalysisMessagesToConsole;
			surrogate.WritePredictReportsToConsole = printSurrogatePredictionMessagesToConsole;

			ISolutionPredictionStrategy solutionPrediction;
			if (enableSurrogate)
			{
				solutionPrediction = surrogate;
			}
			else
			{
				if (useSolutionFromPreviousStep)
				{
					solutionPrediction = new SolutionOfPreviousTimestepAsPrediction();
				}
				else
				{
					solutionPrediction = new NullSolutionPredictionStrategy();
				}
			}

			var solverFactory = new DynamicAmgAiSolver.Factory(numAnalysesForTraining, numPrincipalComponents, solutionPrediction);
			solverFactory.DofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			solverFactory.TrainingStrategy = new BulkSolutionsTrainingStrategy(timeStepSavePeriod);
			//solverFactory.TrainingStrategy = new SeparateTimeStepSolutionsTrainingStrategy(numTimeSteps);
			solverFactory.KeepOnlyNonZeroPrincipalComponents = true;
			solverFactory.AlwaysUseInitialPreconditioner = useAlwaysInitialPreconditioner;
			solverFactory.PcgMaxIterationsProvider = new PercentageMaxIterationsProvider(1.0);
			solverFactory.PcgConvergenceTolerance = pcgTol;
			if (pcgConvergenceBasedOnResidualOnly)
			{
				solverFactory.PcgConvergenceStrategy = new PureResidualConvergence();
			}

			DynamicAmgAiSolver solver = solverFactory.BuildSolver();

			this.solver = solver;
		}

		public StochasticAnalysisRunner PrepareStochasticAnalysis(int numAnalysesTotal, int numAnalysesForTraining)
		{
			var runner = new StochasticAnalysisRunner(this, rngSeed);
			runner.PrintMessagesToConsole = printAnalysisMessagesToConsole;

			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "NumDofs",
				IsConstant = true,
				Format = "F0",
				DescriptionPerAnalysis = "Dofs",
				DescriptionAtEnd = "Dofs",
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "MonitoredDisplacement",
				Format = "E",
				DescriptionPerAnalysis = "Displacement at monitor dof",
				DescriptionAtEnd = "Displacement at monitor dof",
				PrintAverageAtEnd = true,
				PrintStdDevAtEnd = true,
				PrintMinMaxAtEnd = true,
				PrintOnSameLineAsPrevious = true,
			});

			runner.Responses.Add(new ResponseString()
			{
				Name = "Preconditioner",
				DescriptionPerAnalysis = "Preconditioner",
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "PcgIterations",
				Format = "F0",
				DescriptionPerAnalysis = "Average number of PCG iterations per timestep",
				DescriptionAtEnd = "Average number of PCG iterations per timestep",
				PrintAverageAtEnd = true,
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "PreconditionerDuration",
				UnitsDescription = "ms",
				DescriptionPerAnalysis = "Preconditioner calculation duration",
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "PcgSolutionDuration",
				UnitsDescription = "ms",
				Format = "F0",
				DescriptionPerAnalysis = "PCG solution duration (sum of all timesteps)",
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "SolverDuration",
				UnitsDescription = "ms",
				Format = "F0",
				DescriptionPerAnalysis = "Total solver duration (sum of all timesteps)",
				DescriptionAtEnd = "Total solver duration (sum of all timesteps)",
				PrintAverageAtEnd = true,
				PrintOnSameLineAsPrevious = true,
			});
			runner.Responses.Add(new ResponseNumeric()
			{
				Name = "TrainingDuration",
				UnitsDescription = "ms",
				Format = "F0",
				DescriptionPerAnalysis = "Surrogate training duration",
				DescriptionAtEnd = "Surrogate training duration",
				ValueToIgnoreWhenPrinting = 0.0,
				PrintSumAtEnd = true,
				PrintOnSameLineAsPrevious = true,
			});

			runner.RegisterAnalysisGroup(numAnalysesForTraining, "Initial preconditioner");
			runner.RegisterAnalysisGroup(numAnalysesTotal - numAnalysesForTraining, "POD-2G preconditioner");
			if (saveLoadOrNotPretrainingAnalyses.ToString().ToUpper() == "S")
			{
				runner.SaveFirstAnalyses(numAnalysesForTraining, workDirectory);
			}
			else if (saveLoadOrNotPretrainingAnalyses.ToString().ToUpper() == "L")
			{
				runner.LoadFirstAnalyses(numAnalysesForTraining, workDirectory);
			}

			return runner;
		}

		private static double RunDynamicAnalysis(Model model, IAlgebraicModel algebraicModel, ISolver solver, int monitorNodeId)
		{
			var problem = new ProblemStructural(model, algebraicModel);

			var linearAnalyzer = new LinearAnalyzer(algebraicModel, solver, problem);
			var dynamicAnalyzerBuilder = new NewmarkDynamicAnalyzer.Builder(algebraicModel, problem, linearAnalyzer,
				timeStepSize, timeStepSize * numTimeSteps, calculateInitialDerivativeVectors: false);
			dynamicAnalyzerBuilder.SetNewmarkParametersForConstantAcceleration();
			var dynamicAnalyzer = dynamicAnalyzerBuilder.Build();

			dynamicAnalyzer.Initialize();
			dynamicAnalyzer.Solve();

			INode monitorNode = model.GetNode(monitorNodeId);
			double displ = algebraicModel.ExtractSingleValue(
				solver.LinearSystem.Solution, monitorNode, StructuralDof.TranslationX);
			return displ;
		}

		public Dictionary<string, object> RunSingleAnalysis(int analysisId)
		{
			(Model model, double[] parameters, int monitorNodeId) = example.CreateFemModel();

			SolverLogger solverLogger;
			double monitorDisplacement;
			int numDofs;
			string preconditionerName;
			int numPcgIterations;
			if (useDirectSolverInstead)
			{
				var algebraicModel = directSolverFactory.BuildAlgebraicModel(model);
				SkylineSolver directSolver = directSolverFactory.BuildSolver(algebraicModel);
				solverLogger = (SolverLogger)(directSolver.Logger);
				monitorDisplacement = RunDynamicAnalysis(model, algebraicModel, directSolver, monitorNodeId);
				numDofs = directSolver.LinearSystem.Solution.SingleVector.Length;
				preconditionerName = "SkylineSolver";
				numPcgIterations = 0;
			}
			else
			{
				this.solver.SetModel(analysisId, parameters, model);
				solverLogger = this.solver.Logger;
				monitorDisplacement = RunDynamicAnalysis(model, solver.AlgebraicModel, solver, monitorNodeId);
				numDofs = this.solver.LinearSystem.Solution.SingleVector.Length;
				preconditionerName = this.solver.CurrentPreconditionerName;

				numPcgIterations = 0;
				for (int t = 0; t < numTimeSteps; t++)
				{
					numPcgIterations += solver.Logger.GetNumIterationsOfIterativeAlgorithm(t);
				}
			}

			solverLogger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.UpdatePreconditioner.ToString(), out long precCalcDuration);
			solverLogger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.SolveWithPcg.ToString(), out long solveDuration);
			solverLogger.TryGetTaskDuration(DynamicAmgAiSolver.Subtask.TrainML.ToString(), out long trainingDuration);

			var results = new Dictionary<string, object>();
			results["MonitoredDisplacement"] = monitorDisplacement;
			results["NumDofs"] = numDofs;
			results["Preconditioner"] = preconditionerName;
			results["PcgIterations"] = Math.Round(((double)numPcgIterations) / numTimeSteps);
			results["PreconditionerDuration"] = precCalcDuration;
			results["PcgSolutionDuration"] = solveDuration;
			results["SolverDuration"] = precCalcDuration + solveDuration;
			results["TrainingDuration"] = trainingDuration;

			return results;
		}

		public void LoadState()
		{
			solver.LoadState(workDirectory);
		}

		public void SaveState()
		{
			solver.SaveState(workDirectory);
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

		private static void PrintModelParams(double[] modelParams)
		{
			var msg = new StringBuilder();
			msg.AppendLine();
			msg.Append("Model parameters: ");
			foreach (double p in modelParams)
			{
				msg.Append(p);
				msg.Append(" ");
			}
			msg.AppendLine();
			Console.Write(msg);
		}
	}
}
#pragma warning restore CA1305 // Specify IFormatProvider
#pragma warning restore SA1516 // Elements should be separated by blank line
