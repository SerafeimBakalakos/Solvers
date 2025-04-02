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
	using MGroup.Solvers.MachineLearning.StochasticExtensions.Statistics;
	using MGroup.Solvers.MachineLearning.Tests.StochasticExtensions;
	using MGroup.Solvers.MachineLearning.Tests.Utilities;

	public class SingleDofAnalysis
	{
		// Paths
		private const bool runOnCluster = false;
		private const string workDirectory = runOnCluster ?
			"M:\\Serafeim\\results\\SingleDof"
			: "C:\\Users\\Serafeim\\Desktop\\AISolve\\SingleDof";

		// Number of analyses
		private const int numAnalysesForTraining = 500; // originally 450 (x60 = 27000)
		private const int numAnalysesForValidation = 125; // e.g. train / validation / test set = 60% / 20% / 20%
		private const int numAnalysesForTesting = 10000; // originally 350
		private const int numAnalysesTotal = numAnalysesForTraining + numAnalysesForTesting; // originally 800 (x60 = 48000)
		private const int numTimeSteps = 100; // originally 60
		private const double timeStepSize = 0.04264;


		// Model
		private const double elasticityModulusMean = 20E6;
		private const double elasticityModulusStdDev = 1E6;
		private const double mass = 10; // 10 ton
		private const double dampingRatio = 0.05;
		private const double momentInertia = 2*1.6E-3; // 2 beams/columns (30cm x 40cm)
		private const double beamLength = 5; // Originally 2.5 m
		private const double externalLoadCyclicFrequency = 14.736;
		private const double externalLoadMax = 5; // 5kN
		private const bool isExternalLoadRamp = false;

		// Surrogate
		private const string normalizationForModelParams = "MinMax"; // Choose from "Null", "MinMax", "MinMaxWithoutShifting", "Zscore"
		private const string normalizationForSolutions = "MinMax";

		// Misc
		private const int rngSeedForTrainSet = 23;
		private const int rngSeedForTestSet = 17;
		private const int rngSeedForValidationSet = 31;

		public static void RunAllAnalysesAndSaveModelParamsAndTimeHistory()
		{
			// Train set
			var rng = new RepeatableRandom(rngSeedForTrainSet);
			var stochasticAnalysis = new SingleDofAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var trainDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTraining);
			bool timestepAsParam = numTimeSteps > 1;
			float[,] modelParams = trainDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			float[,] solutions = trainDB.ToFloatArray2DAllSolutionsAsRows(true);
			WriteTrainingData(modelParams, solutions, DataSetType.Train);

			// Test set
			rng = new RepeatableRandom(rngSeedForTestSet);
			stochasticAnalysis = new SingleDofAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var testDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForTesting);
			modelParams = testDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			solutions = testDB.ToFloatArray2DAllSolutionsAsRows(true);
			WriteTrainingData(modelParams, solutions, DataSetType.Test);

			//double error = surrogate.CalcPodReconstructionError(testDB);
			//Console.WriteLine($"POD reconstruction error = {error}");

			// Validation set
			rng = new RepeatableRandom(rngSeedForValidationSet);
			stochasticAnalysis = new SingleDofAnalysis();
			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			var validationDB = RunAndSaveAnalyses(stochasticAnalysis, numAnalysesForValidation);
			modelParams = validationDB.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsParam, true);
			solutions = validationDB.ToFloatArray2DAllSolutionsAsRows(true);
			WriteTrainingData(modelParams, solutions, DataSetType.Validation);
		}

		private static SolutionDatabaseDynamic RunAndSaveAnalyses(SingleDofAnalysis stochasticAnalysis, int numAnalyses)
		{
			var solutionDB = new SolutionDatabaseDynamic();
			for (int i = 0; i < numAnalyses; i++)
			{
				Console.WriteLine($"Analysis {i + 1}/{numAnalyses}");

				double E = stochasticAnalysis.elasticityDistribution.GenerateSample();
				(SingleDofModel model, double[] parameters, List<Vector> solutions) = stochasticAnalysis.RunSingleAnalysis();

				solutionDB.SaveModelParameters(i, parameters);
				for (int t = 0; t < numTimeSteps; t++)
				{
					solutionDB.SaveSolution(i, t, solutions[t]);
				}

				//Console.WriteLine($"u(dof0, maxTimestep) = {solverLogger.GetSolutionVector(numTimeSteps - 1)[0]}");
			}

			return solutionDB;
		}

		private IDistribution elasticityDistribution;

		public SingleDofAnalysis()
		{
		}

		public ISolutionPredictionStrategy SolutionPredictionStrategy { get; private set; }

		public void InitializeModel(RepeatableRandom rng)
		{
			this.elasticityDistribution = 
				NormalDistribution.CreateWithMeanStddev(rng, elasticityModulusMean, elasticityModulusStdDev);
		}

		public void InitializeSolver() { }
		public void LoadState() { }
		public void SaveState() { }

		public (SingleDofModel model, double[] parameters, List<Vector> solutions) RunSingleAnalysis()
		{
			double E = elasticityDistribution.GenerateSample();
			double[] parameters = { E };
			double stiffness = E * momentInertia / Math.Pow(beamLength, 3);

			SingleDofModel model;
			List<Vector> solutions;
			if (isExternalLoadRamp == false)
			{
				model = SingleDofModel.CreateWithHarmonicLoad(stiffness, mass, dampingRatio, 
					externalLoadMax, externalLoadCyclicFrequency);
				solutions = model.CalcDisplacementHistory(0, timeStepSize, numTimeSteps);
			}
			else
			{
				throw new NotImplementedException();
			}

			return (model, parameters, solutions);
		}

		private static void WriteTrainingData(float[,] modelParams, float[,] solutions, DataSetType dataSet)
		{
			INormalizationStrategy normalizationOfInput = ChooseNormalization(normalizationForModelParams);
			normalizationOfInput.InitializeAndApply(modelParams);

			INormalizationStrategy normalizationOfOutput = ChooseNormalization(normalizationForSolutions);
			normalizationOfOutput.InitializeAndApply(solutions);

			if (!Directory.Exists(workDirectory))
			{
				throw new IOException($"Directory {workDirectory} does not exist");
			}

			string directory = Path.Combine(workDirectory, "python_experimenting");
			Directory.CreateDirectory(directory);
			string prefix = WriteDataSetPrefix(dataSet);
			string pathModelParams = Path.Combine(directory, prefix + "_model_params.npy");
			string pathPodCoeffs = Path.Combine(directory, prefix + "_solutions.npy");

			IArrayFileIO arrayIO = new ArrayBinaryFileIO();
			//IArrayFileIO arrayIO = new ArrayTextFileIO();
			arrayIO.WriteArray2DToFile(modelParams, pathModelParams);
			arrayIO.WriteArray2DToFile(solutions, pathPodCoeffs);
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
