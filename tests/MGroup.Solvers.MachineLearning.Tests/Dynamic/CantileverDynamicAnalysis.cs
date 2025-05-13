#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable SA1516 // Elements should be separated by blank line
namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
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
	using MGroup.Solvers.MachineLearning.MLExtensions;
	using MGroup.Solvers.MachineLearning.MLExtensions.Normalization;
	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;
	using MGroup.Solvers.MachineLearning.MLExtensions.Utilities;
	using MGroup.Solvers.MachineLearning.Plotting;
	using MGroup.Solvers.MachineLearning.PodAmg;
	using MGroup.Solvers.MachineLearning.PodAmg.Surrogates;
	using MGroup.Solvers.MachineLearning.StochasticExtensions;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.KarhunenLoeve;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.RandomNumberGeneration;
	using MGroup.Solvers.MachineLearning.Tests.StochasticExtensions;
	using MGroup.Solvers.MachineLearning.Tests.Utilities;

	public class CantileverDynamicAnalysis : IAutoStochasticAnalysis
	{
		// Paths
		private static int machineID = 0; // 0 = Serafeim's local machine, 1 = cluster (Serafeim folders on 204), 2 = cluster (Atzarakis folders on 207)
		private static string workDirectory;
		private static string pythonProjectDirectory;
		private static string pythonInterpreter;
		private static string trainScriptCaeFffnn;
		private static string trainScriptPodFffnn;
		private static string predictScriptCaeFfnn;
		private static string predictScriptPodFfnn;

		static CantileverDynamicAnalysis()
		{
			if (machineID == 0) // Serafeim's local machine
			{
				workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear";
				pythonProjectDirectory = "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates";
				pythonInterpreter = pythonProjectDirectory + "\\venv\\Scripts\\python.exe";
				trainScriptCaeFffnn = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\train.py";
				trainScriptPodFffnn = pythonProjectDirectory + "\\src\\pod_ffnn_dynamic_t_as_param\\train.py";
				predictScriptCaeFfnn = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\predict.py";
				predictScriptPodFfnn = pythonProjectDirectory + "\\src\\pod_ffnn_dynamic_t_as_param\\predict.py";
			}
			else if (machineID == 1) // cluster (Serafeim folders on 204)
			{
				workDirectory = "M:\\Serafeim\\results\\CantileverDynamicLinear";
				pythonProjectDirectory = "C:\\Users\\cluster\\Desktop\\Serafeim\\code\\Python\\cs2py_ml_surrogates";
				pythonInterpreter = pythonProjectDirectory + "\\venv\\Scripts\\python.exe";
				trainScriptCaeFffnn = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\train.py";
				trainScriptPodFffnn = pythonProjectDirectory + "\\src\\pod_ffnn_dynamic_t_as_param\\train.py";
				predictScriptCaeFfnn = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\predict.py";
				predictScriptPodFfnn = pythonProjectDirectory + "\\src\\pod_ffnn_dynamic_t_as_param\\predict.py";
			}
			else if (machineID == 2) // cluster(Atzarakis folders on 207)
			{
				workDirectory = "M:\\shared\\Serafeim_Atzarakis\\results\\CantileverDynamicLinear";
				pythonProjectDirectory = "C:\\Users\\cluster\\constantinos\\dl-project\\dl-experiments";
				pythonInterpreter = pythonProjectDirectory + "\\.venv\\Scripts\\python.exe";
				trainScriptCaeFffnn = null;
				trainScriptPodFffnn = null;
				predictScriptCaeFfnn = pythonProjectDirectory + "";
				predictScriptPodFfnn = null;
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		// Number of analyses
		private const int numAnalysesForTraining = 1000; // originally 450 (x60 = 27000)
		private const int numAnalysesForValidation = 500; // e.g. train / validation / test set = 60% / 20% / 20%
		private const int numAnalysesForTesting = 1000; // originally 350
		private const int numAnalysesTotal = numAnalysesForTraining + numAnalysesForTesting; // originally 800 (x60 = 48000)
		private const int numTimeSteps = 200; // originally 60
		private const double timeStepSize = 0.05;

		// Model: geometry
		//private static readonly int[] numElements = { 35, 140 }; //10080 dofs
		private static readonly int[] numElements = { 5, 25 }; //300 dofs
															   //private static readonly int[] numElements = { 16, 80 };
															   //private static readonly int[] numElements = { 32, 160 };
		private const double beamLength = 10;
		private const double beamSectionHeight = 2.0;
		private const double beamSectionWidth = 1.0;

		// Model: material
		private const double elasticityModulusMean = 200E6;
		private const double elasticityModulusStdDev = 10E6;
		private const string elasticityFieldType = "KL"; // Valid inputs: "KL"=Karhunen-Loeve, "WN"=white noise, "HG"=homogeneous
		private const int numKarhunenLoeveTerms = 6; // Originally 6.
		private const double correlationLength = 2 * beamLength; // originally 0.5 * beamLength
		private const bool nodalLoadIsConcentrated = true;
		private const double materialDensity = 0.01;

		// Model: loads
		private const double externalLoadCyclicFrequency = 15; // sin(omega*t + phi). Originally omega=15 
		private const double externalLoadPhaseDiff = Math.PI / 2; // sin(omega*t + phi). Originally phi=pi/2 
		private const CantileverDynamicModel.LoadType externalLoadType = CantileverDynamicModel.LoadType.Harmonic; // Originally LoadType.Harmonic

		// Solver: general
		private const double pcgTol = 1E-6;
		private const bool pcgConvergenceBasedOnResidualOnly = true;
		private static bool useDirectSolverInstead = false;

		// Solver: POD preconditioner
		private const int numPrincipalComponents = 5; // 1 (not that effective), 5, 10 (start here), 15, 20 (doubtful)
		private const int timeStepSavePeriod = 5; // 1 (too expensive), 5 (good), 10 (good), 15, 20
		private const bool useAlwaysInitialPreconditioner = false;

		// Surrogate
		private const string surrogateType = "PodFfnn"; // Options: "CaeFfnn", "PodFfnn", "None"
		private const bool useSolutionDifferenceFromPreviousStep = false;
		private const bool useBinaryIOFiles = true;
		private const bool batchTimeHistoryPredictions = true;
		private const string normalizationForModelParams = "Null"; // Choose from "Null", "MinMax", "MinMaxWithoutShifting", "Zscore"
		private const string normalizationForSolutions = "Null";
		private const string normalizationForPodCoeffs = "MinMax";
		private const int numSurrogatePodPrincipalComponents = 1;
		private const int numSurrogateKLTerms = 1; // 0 = use the same as numKarhunenLoeveTerms
		private static readonly int surrogatePodTimeStepSavePeriod = Math.Min(5, timeStepSavePeriod);
		//TODO: option to read models from files, instead of creating them from start
		//TODO: option to predict initial solutions for all timesteps (of the same dynamic analysis) at once, instead of each timestep separately. This will greatly reduce communication overheads

		// Reports
		private const bool printAnalysisMessagesToConsole = true;
		private const bool printSurrogatePredictionMessagesToConsole = false;

		// Misc
		private const char saveLoadOrNotPretrainingAnalyses = 'S'; // 'S' for save, 'L' for load, anything else for neither.
		private const bool readMLNetworksFromFileWithoutTraining = false;
		private const double exactSolutionPercentageForPrediction = 0;
		private const int rngSeedForTrainSet = 23;
		private const int rngSeedForTestSet = 17;
		private const int rngSeedForValidationSet = 31;

		private static CaeFfnnArchitecture DescribeCaeFfnnSurrogate(int[] numElementsPerAxis, int numModelParameters)
		{
			int ffnnHiddenSize = 32;

			var arch = new CaeFfnnArchitecture();

			arch.NumDofs = 2 * (numElementsPerAxis[0] + 1) * numElementsPerAxis[1]; //200
			arch.NumModelParams = (numTimeSteps > 1) ? numModelParameters + 1 : numModelParameters;
			arch.LatentSpaceDim = 8;

			arch.CaeLearningRateStart = 1E-3f;
			arch.CaeLearningRateEnd = 1E-4f;
			arch.CaeNumEpochs = 250;
			arch.CaeBatchSize = 20;

			arch.FfnnLearningRateStart = 1E-3f;
			arch.FfnnLearningRateEnd = 1E-5f;
			arch.FfnnNumEpochs = 4200; //3000 took too long for 81 model params
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
			arch.DecoderLayers.Add(new DenseLayer(units: 16));
			arch.DecoderLayers.Add(new LeakyReLULayer());
			arch.DecoderLayers.Add(new ReshapeLayer(new int[] { 1, 16 }));
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

		private static FfnnArchitecture DescribeFfnnSurrogate()
		{
			int ffnnHiddenSize = 64;

			var arch = new FfnnArchitecture();

			arch.NumPodBasisVectors = numSurrogatePodPrincipalComponents;
			if (numSurrogateKLTerms > 0)
			{
				arch.NumModelParams = (numTimeSteps > 1) ? numSurrogateKLTerms + 1 : numSurrogateKLTerms;
			}
			else
			{
				arch.NumModelParams = (numTimeSteps > 1) ? numKarhunenLoeveTerms + 1 : numKarhunenLoeveTerms;
			}

			arch.FfnnLearningRateStart = 1E-3f;
			arch.FfnnLearningRateEnd = 1E-5f;
			arch.FfnnNumEpochs = 5000; //5000 is more than enough
			arch.FfnnBatchSize = 20;

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
			arch.FfnnLayers.Add(new DenseLayer(units: numSurrogatePodPrincipalComponents));

			return arch;
		}

		public static void RunStochasticAnalysis()
		{
			var analysis = new CantileverDynamicAnalysis();
			StochasticAnalysisRunner runner = analysis.PrepareStochasticAnalysis(numAnalysesTotal, numAnalysesForTraining);
			runner.RunAll();
		}

		public static void RunAllAnalysesAndSaveSolutions()
		{
			useDirectSolverInstead = true;
			bool treatTimeAsModelParam = false;

			var rng = new RepeatableRandom(rngSeedForTrainSet);
			var stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var trainDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTraining);
			WriteTrainingDataForCaeFfnn(trainDB, DataSetType.Train, treatTimeAsModelParam);

			rng = new RepeatableRandom(rngSeedForTestSet);
			stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var testDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTesting);
			WriteTrainingDataForCaeFfnn(testDB, DataSetType.Test, treatTimeAsModelParam);

			rng = new RepeatableRandom(rngSeedForValidationSet);
			stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var validationDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForValidation);
			WriteTrainingDataForCaeFfnn(validationDB, DataSetType.Validation, treatTimeAsModelParam);
		}

		public static void RunAllAnalysesAndSaveModelParamsAndPodCoeffs()
		{
			useDirectSolverInstead = true;

			// Train set
			var rng = new RepeatableRandom(rngSeedForTrainSet);
			var stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();
			var surrogate = (PodFfnnSurrogateDynamicPythonTF)stochasticAnalysis.SolutionPredictionStrategy;

			var trainDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTraining);
			bool timestepAsParam = numTimeSteps > 1;
			float[,] modelParams = trainDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			surrogate.TrainPod(trainDB);  //TODO: Perhaps the validation set can be included here
			float[,] podCoeffs = surrogate.CompressSolutionVectors(trainDB);
			WriteTrainingDataForPodFfnn(modelParams, podCoeffs, DataSetType.Train);

			// Test set
			rng = new RepeatableRandom(rngSeedForTestSet);
			stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var testDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTesting);
			modelParams = testDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			podCoeffs = surrogate.CompressSolutionVectors(testDB);
			WriteTrainingDataForPodFfnn(modelParams, podCoeffs, DataSetType.Test);

			double error = surrogate.CalcPodReconstructionError(testDB);
			Console.WriteLine($"POD reconstruction error = {error}");

			// Validation set
			rng = new RepeatableRandom(rngSeedForValidationSet);
			stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var validationDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForValidation);
			modelParams = validationDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			podCoeffs = surrogate.CompressSolutionVectors(validationDB);
			WriteTrainingDataForPodFfnn(modelParams, podCoeffs, DataSetType.Validation);
		}

		private static SolutionDatabaseDynamic RunAndSaveAnalyses(CantileverDynamicAnalysis stochasticAnalysis, int numAnalyses)
		{
			var solutionDB = new SolutionDatabaseDynamic();
			for (int i = 0; i < numAnalyses; i++)
			{
				Console.WriteLine($"Analysis {i + 1}/{numAnalyses}");
				(Model model, double[] parameters, int monitorNodeId) = stochasticAnalysis.exampleModel.CreateFemModel();

				var algebraicModel = stochasticAnalysis.directSolverFactory.BuildAlgebraicModel(model);
				SkylineSolver directSolver = stochasticAnalysis.directSolverFactory.BuildSolver(algebraicModel);
				directSolver.LogSolutionVectors = true;
				RunDynamicAnalysis(model, algebraicModel, directSolver, monitorNodeId);

				SolverLogger solverLogger = (SolverLogger)(((ISolver)directSolver).Logger);

				solutionDB.SaveModelParameters(i, parameters);
				for (int t = 0; t < numTimeSteps; t++)
				{
					solutionDB.SaveSolution(i, t, solverLogger.GetSolutionVector(t));
				}

				//Console.WriteLine($"u(dof0, maxTimestep) = {solverLogger.GetSolutionVector(numTimeSteps - 1)[0]}");
			}

			return solutionDB;
		}

		public static void CalcSurrogateError()
		{
			int numDofs = 2 * (numElements[0] + 1) * (numElements[1] + 1 - 1);

			// Paths
			string directory = workDirectory;
			//string directory = Path.Combine(workDirectory, "python_experimenting");
			Directory.CreateDirectory(directory);
			bool testData = true;
			string prefix = testData ? "test" : "train";
			string pathModelParams = Path.Combine(directory, prefix + "_model_params.npy");
			string pathSolutions = Path.Combine(directory, prefix + "_solutions.npy");

			// Create train and test databases
			//TODO: Read the DBs from files
			Console.WriteLine("Running all analyses to obtain the train and test datasets");
			var rng = new RepeatableRandom(rngSeedForTrainSet);
			useDirectSolverInstead = true;
			var stochasticAnalysis = new CantileverDynamicAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();
			var trainDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTraining);
			var testDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTesting);

			// Surrogate settings
			Console.WriteLine("Initializing surrogate");
			int numModelParams = numKarhunenLoeveTerms;
			if (surrogateType != "CaeFfnn")
			{
				throw new NotImplementedException();
			}
			CaeFfnnArchitecture architecture = DescribeCaeFfnnSurrogate(numElements, numModelParams);
			bool timestepAsModelParam = numTimeSteps > 1;
			var surrogate = new CaeFfnnSurrogateDynamicPythonTF(architecture, workDirectory, pythonModelID: 43,
				timestepAsModelParam);
			//surrogate.float64 = false;
			surrogate.NormalizationOfParameters = ChooseNormalization(normalizationForModelParams);
			surrogate.NormalizationOfSolutions = ChooseNormalization(normalizationForSolutions);
			surrogate.BatchTimeHistoryPredictions = batchTimeHistoryPredictions;
			surrogate.TensorFlowSeed = rngSeedForTrainSet;
			surrogate.Splitter.MinTestSetPercentage = 0.0; // Set it to something that encompasses all timesteps of the affected parameter realizations
			surrogate.Splitter.MinValidationSetPercentage = 0.0; // This stays 0
			surrogate.UseBinaryIOFilesForArrays = useBinaryIOFiles;
			surrogate.UseSolutionDifferenceFromPreviousStep = useSolutionDifferenceFromPreviousStep;
			surrogate.WriteTrainReportToConsole = false;
			surrogate.WritePredictReportsToConsole = true;
			surrogate.ReadMLNetworksFromFilesWithoutTraining = true;
			if (surrogateType == "PodFfnn")
			{
				surrogate.SetPythonCodePaths(pythonInterpreter, trainScriptPodFffnn, predictScriptPodFfnn);

			}
			else
			{
				surrogate.SetPythonCodePaths(pythonInterpreter, trainScriptCaeFffnn, predictScriptCaeFfnn);
			}

			// Train surrogate
			Console.WriteLine("Training the surrogate");
			surrogate.Train(trainDB);

			//// Read files
			//Console.WriteLine("Reading from files test samples for model parameters and solution vectors");
			//IArrayFileIO arrayIO = new ArrayBinaryFileIO();
			//float[,] testInputs = arrayIO.ReadArray2DFromFile(pathModelParams);
			//float[,] testOutputs = arrayIO.ReadArray2DFromFile(pathSolutions);
			//int numTestSamples = testInputs.GetLength(0);
			//if (testOutputs.GetLength(0) != numTestSamples)
			//{
			//	throw new Exception("Different number of test input samples than output samples");
			//}
			//if (testInputs.GetLength(1) != numModelParams + 1)
			//{
			//	throw new Exception("Different number of test inputs than model parameters");
			//}
			//if (testOutputs.GetLength(1) != numDofs)
			//{
			//	throw new Exception("Different number of test outputs than dofs");
			//}

			// Calculate error
			int numTestSamples = testDB.CountParameterSets() * testDB.CountTimeSteps();
			Console.WriteLine($"Calculating surrogate error. {numTestSamples} samples in total.");
			double meanError = 0;

			int sample = 0;
			foreach (int paramSetID in testDB.EnumerateParameterSetIDs())
			{
				foreach (int t in testDB.EnumerateTimeSteps())
				{
					if (batchTimeHistoryPredictions)
					{
						if (t == 0)
						{
							Console.Write($"Samples {sample} - {sample + numTimeSteps}: ");
						}
					}
					else
					{
						Console.Write($"Sample {sample}: ");
					}
					sample++;

					Vector expectedSolution = testDB.GetSolution(paramSetID, t);
					double[] modelParams = testDB.GetModelParameters(paramSetID);

					var prediction = Vector.CreateFromArray(surrogate.Predict(t, modelParams));
					meanError += (prediction - expectedSolution).Norm2() / expectedSolution.Norm2();
				}
			}

			//for (int i = 0; i < numTestSamples; i++)
			//{
			//	if (i % 5 == 0)
			//	{
			//		Console.Write(i + " ");
			//	}
			//	//int t = (int)(testInputs[i, 0]);
			//	var modelParams = new double[numModelParams];
			//	for (int j = 0; j < numModelParams; j++)
			//	{
			//		modelParams[j] = testInputs[i, j + 1];
			//	}

			//	var solution = new double[numDofs];
			//	for (int j = 0; j < numDofs; j++)
			//	{
			//		solution[j] = testOutputs[i, j];
			//	}

			//	var expected = Vector.CreateFromArray(solution);
			//	var prediction = Vector.CreateFromArray(surrogate.Predict(t, modelParams));

			//	meanError += (prediction - expected).Norm2() / expected.Norm2();
			//}

			meanError /= numTestSamples;
			Console.WriteLine();
			Console.WriteLine("CAE-FFNN surrogate mean error on test set (|expected - predicted| / |expected| = " + meanError);
		}

		private CantileverDynamicModel exampleModel;
		private DynamicAmgAiSolver solver;
		private SkylineSolver.Factory directSolverFactory;

		public CantileverDynamicAnalysis()
		{
		}

		public ISolutionPredictionStrategy SolutionPredictionStrategy { get; private set; }

		public void InitializeModel(RepeatableRandom rng)
		{
			IRandomField1D elasticityField = DefineElasticityField(numElements, rng);
			var example = CantileverDynamicModel.Create2DExample(numElements[0], numElements[1], elasticityField);
			example.SetTimeSteps(numTimeSteps * timeStepSize, numTimeSteps);
			example.BeamLength = beamLength;
			example.BeamSectionHeight = beamSectionHeight;
			example.BeamSectionWidth = beamSectionWidth;
			example.NodalLoadIsConcentrated = nodalLoadIsConcentrated;
			example.Density = materialDensity;
			example.ExternalLoadType = externalLoadType;
			example.ExternalLoadCyclicFrequency = externalLoadCyclicFrequency;
			example.ExternalLoadPhaseDiff = externalLoadPhaseDiff;

			this.exampleModel = example;
		}

		public void InitializeSolver()
		{
			CreateSolutionPredictionStrategy();

			if (useDirectSolverInstead)
			{
				directSolverFactory = new SkylineSolver.Factory();
				return;
			}

			var solverFactory = new DynamicAmgAiSolver.Factory(numAnalysesForTraining, numPrincipalComponents, SolutionPredictionStrategy);
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
			solver.ExactSolutionPercentageForPrediction = exactSolutionPercentageForPrediction;

			this.solver = solver;
		}

		public StochasticAnalysisRunner PrepareStochasticAnalysis(int numAnalysesTotal, int numAnalysesForTraining)
		{
			var runner = new StochasticAnalysisRunner(this, rngSeedForTrainSet);
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

		public Dictionary<string, object> RunSingleAnalysis(int analysisId)
		{
			(Model model, double[] parameters, int monitorNodeId) = exampleModel.CreateFemModel();

			SolverLogger solverLogger;
			double monitorDisplacement;
			int numDofs;
			string preconditionerName;
			int numPcgIterations;
			if (useDirectSolverInstead)
			{
				var algebraicModel = directSolverFactory.BuildAlgebraicModel(model);
				SkylineSolver directSolver = directSolverFactory.BuildSolver(algebraicModel);
				solverLogger = (SolverLogger)(((ISolver)directSolver).Logger);
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

		private void CreateSolutionPredictionStrategy()
		{
			if (surrogateType == "CaeFfnn")
			{
				CaeFfnnArchitecture architecture = DescribeCaeFfnnSurrogate(numElements, exampleModel.NumModelParameters);
				bool timestepAsModelParam = numTimeSteps > 1;
				var surrogate = new CaeFfnnSurrogateDynamicPythonTF(architecture, workDirectory, pythonModelID: 43,
					timestepAsModelParam);
				//surrogate.float64 = false;
				surrogate.NormalizationOfParameters = ChooseNormalization(normalizationForModelParams);
				surrogate.NormalizationOfSolutions = ChooseNormalization(normalizationForSolutions);
				surrogate.TensorFlowSeed = rngSeedForTrainSet;
				surrogate.BatchTimeHistoryPredictions = batchTimeHistoryPredictions;
				surrogate.Splitter.MinTestSetPercentage = 0.0; // Set it to something that encompasses all timesteps of the affected parameter realizations
				surrogate.Splitter.MinValidationSetPercentage = 0.0; // This stays 0
				surrogate.SetPythonCodePaths(pythonInterpreter, trainScriptCaeFffnn, predictScriptCaeFfnn);
				surrogate.UseBinaryIOFilesForArrays = useBinaryIOFiles;
				surrogate.UseSolutionDifferenceFromPreviousStep = useSolutionDifferenceFromPreviousStep;
				surrogate.WriteTrainReportToConsole = printAnalysisMessagesToConsole;
				surrogate.WritePredictReportsToConsole = printSurrogatePredictionMessagesToConsole;
				surrogate.ReadMLNetworksFromFilesWithoutTraining = readMLNetworksFromFileWithoutTraining;
				SolutionPredictionStrategy = surrogate;
			}
			else if (surrogateType == "PodFfnn")
			{
				FfnnArchitecture architecture = DescribeFfnnSurrogate();
				bool timestepAsModelParam = numTimeSteps > 1;
				var surrogate = new PodFfnnSurrogateDynamicPythonTF(architecture, workDirectory, pythonModelID: 43,
					timestepAsModelParam);
				//surrogate.float64 = false;
				surrogate.NumRequestedPodPrincipalComponents = numSurrogatePodPrincipalComponents;
				surrogate.NumRequestedKarhunenLoeveTerms = numSurrogateKLTerms;
				surrogate.PodTimeStepPediod = surrogatePodTimeStepSavePeriod;
				surrogate.NormalizationOfParameters = ChooseNormalization(normalizationForModelParams);
				surrogate.NormalizationOfPodCoeffs = ChooseNormalization(normalizationForPodCoeffs);
				surrogate.TensorFlowSeed = rngSeedForTrainSet;
				surrogate.BatchTimeHistoryPredictions = batchTimeHistoryPredictions;
				//surrogate.Splitter.MinTestSetPercentage = 0.0; // Set it to something that encompasses all timesteps of the affected parameter realizations
				//surrogate.Splitter.MinValidationSetPercentage = 0.0; // This stays 0
				surrogate.SetPythonCodePaths(pythonInterpreter, trainScriptPodFffnn, predictScriptPodFfnn);
				surrogate.UseBinaryIOFilesForArrays = useBinaryIOFiles;
				surrogate.UseSolutionDifferenceFromPreviousStep = useSolutionDifferenceFromPreviousStep;
				surrogate.WriteTrainReportToConsole = printAnalysisMessagesToConsole;
				surrogate.WritePredictReportsToConsole = printSurrogatePredictionMessagesToConsole;
				surrogate.ReadMLNetworksFromFilesWithoutTraining = readMLNetworksFromFileWithoutTraining;
				surrogate.FfnnIOData = new SurrogateIODatabase();
				surrogate.WriteFfnnIoToDirectoryForMatlab = workDirectory;
				SolutionPredictionStrategy = surrogate;
			}
			else
			{
				if (useSolutionDifferenceFromPreviousStep)
				{
					SolutionPredictionStrategy = new SolutionOfPreviousTimestepAsPrediction();
				}
				else
				{
					SolutionPredictionStrategy = new NullSolutionPredictionStrategy();
				}
			}
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
			if (elasticityFieldType == "KL")
			{
				int numNodesAlongBeamLength = numElementsPerAxis[numElementsPerAxis.Length - 1] + 1;

				var elasticityField = new KarhunenLoeveField1D(0, beamLength, numNodesAlongBeamLength,
					elasticityModulusMean, elasticityModulusStdDev, correlationLength, numKarhunenLoeveTerms, rng);
				return elasticityField;
			}
			else if (elasticityFieldType == "WN")
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
			else if (elasticityFieldType == "HG")
			{
				return new RandomHomogeneousField(elasticityModulusMean, elasticityModulusStdDev, rng, true);
			}
			else
			{
				throw new ArgumentException("Invalid elasticity field type");
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

		private static void WriteTrainingDataForCaeFfnn(SolutionDatabaseDynamic solutionDB, DataSetType dataSet, bool treatTimeAsModelParam)
		{
			INormalizationStrategy normalizationParams = ChooseNormalization(normalizationForModelParams);
			INormalizationStrategy normalizationSolutions = ChooseNormalization(normalizationForSolutions);

			if (!Directory.Exists(workDirectory))
			{
				throw new IOException($"Directory {workDirectory} does not exist");
			}

			string directory = Path.Combine(workDirectory, "python_experimenting");
			Directory.CreateDirectory(directory);
			string prefix = WriteDataSetPrefix(dataSet);
			string pathModelParams = Path.Combine(directory, prefix + "_model_params.npy");
			string pathSolutions = Path.Combine(directory, prefix + "_solutions.npy");

			IArrayFileIO arrayIO = new ArrayBinaryFileIO();

			if (treatTimeAsModelParam)
			{
				bool timestepAsParam = numTimeSteps > 1;
				float[,] allParams = solutionDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
				float[,] allSolutions = solutionDB.ToFloatArray2DAllSolutionsAsRows(true);

				normalizationParams.InitializeAndApply(allParams);
				normalizationSolutions.InitializeAndApply(allSolutions);

				arrayIO.WriteArray2DToFile(allParams, pathModelParams);
				arrayIO.WriteArray2DToFile(allSolutions, pathSolutions);
			}
			else
			{
				float[,] allParams = solutionDB.ToFloatArray2DAllParametersAsRows();
				float[,,] allSolutions = solutionDB.ToFloatArray3DSolutions();

				if (!(normalizationParams is NullNormalization && normalizationSolutions is NullNormalization))
				{
					throw new NotImplementedException();
				}

				//normalizationParams.InitializeAndApply(allParams);
				//normalizationSolutions.InitializeAndApply(allSolutions);

				arrayIO.WriteArray2DToFile(allParams, pathModelParams);
				arrayIO.WriteArray3DToFile(allSolutions, pathSolutions);
			}
		}

		private static void WriteTrainingDataForPodFfnn(float[,] modelParams, float[,] podCoeffs, DataSetType dataSet)
		{
			INormalizationStrategy normalizationParams = ChooseNormalization(normalizationForModelParams);
			normalizationParams.InitializeAndApply(modelParams);

			INormalizationStrategy normalizationPodCoeffs = ChooseNormalization(normalizationForPodCoeffs);
			normalizationPodCoeffs.InitializeAndApply(podCoeffs);

			if (!Directory.Exists(workDirectory))
			{
				throw new IOException($"Directory {workDirectory} does not exist");
			}

			string directory = Path.Combine(workDirectory, "python_experimenting");
			Directory.CreateDirectory(directory);
			string prefix = WriteDataSetPrefix(dataSet);
			string pathModelParams = Path.Combine(directory, prefix + "_model_params.npy");
			string pathPodCoeffs = Path.Combine(directory, prefix + "_pod_coeffs.npy");

			IArrayFileIO arrayIO = new ArrayBinaryFileIO();
			arrayIO.WriteArray2DToFile(modelParams, pathModelParams);
			arrayIO.WriteArray2DToFile(podCoeffs, pathPodCoeffs);
		}

		private static INormalizationStrategy ChooseNormalization(string name)
		{
			if (name == "Null")
			{
				return new NullNormalization();
			}
			else if (name == "MinMax")
			{
				return new MinMaxNormalization();
			}
			else if (name == "MinMaxWithoutShifting")
			{
				return new MinMaxWithoutShiftingNormalization();
			}
			else if (name == "Zscore")
			{
				return new ZScoreNormalization();
			}
			else
			{
				throw new Exception("Invalid normalization name");
			}
		}

		private static string WriteDataSetPrefix(DataSetType dataSet)
		{
			if (dataSet == DataSetType.Train)
			{
				return "train";
			}
			else if (dataSet == DataSetType.Test)
			{
				return "test";
			}
			else
			{
				return "validation";
			}
		}
	}
}
#pragma warning restore CA1305 // Specify IFormatProvider
#pragma warning restore SA1516 // Elements should be separated by blank line
