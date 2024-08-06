namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MachineLearning.TensorFlow.KerasLayers;
	using MGroup.MachineLearning.Utilities;
	using MGroup.Solvers.MachineLearning.MLExtensions;
	using MGroup.Solvers.MachineLearning.MLExtensions.Normalization;
	using MGroup.Solvers.MachineLearning.Utilities;

	using Newtonsoft.Json;
	using Tensorflow.IO;

	public class CaeFfnnSurrogateDynamicPythonTF : ISolutionPredictionStrategy
	{
		private readonly bool float64 = false;

		private readonly string workDirectory;
		private readonly int pythonModelID;
		private readonly CaeFfnnArchitecture caeFfnnArch;

		private IArrayFileIO arrayIO = new ArrayBinaryFileIO();
		private SolutionDatabaseDynamic solutionDb;

		private string pythonInterpreter = null;
		private string trainScript = null;
		private string predictScript = null;

		public CaeFfnnSurrogateDynamicPythonTF(CaeFfnnArchitecture caeFfnnArchitecture, string workDirectory, int pythonModelID)
		{
			this.caeFfnnArch = caeFfnnArchitecture;
			this.workDirectory = workDirectory;
			this.pythonModelID = pythonModelID;
			Splitter = new DatasetSplitter();
			Splitter.MinTestSetPercentage = 0.2;
			Splitter.MinValidationSetPercentage = 0.0;
			Splitter.SetOrderToContiguous(DataSubsetType.Training, DataSubsetType.Test);
		}

		/// <summary>
		/// True (default) to delete any files created by this class. False to retain the files for manual inspection.
		/// </summary>
		public bool CleanupIOFiles { get; set; } = true;

		public INormalizationStrategy NormalizationOfParameters { get; set; } = new MinMaxNormalization();

		public INormalizationStrategy NormalizationOfSolutions { get; set; } = new NullNormalization();

		public DatasetSplitter Splitter { get; set; }

		public int TensorFlowSeed { get; set; } = -1;

		/// <summary>
		/// Specifies the milliseconds to wait before aborting the call to a Python script. 
		/// Indefinite waiting if <see cref="TimeoutMilliseconds"/> == -1 (default).
		/// </summary>
		public int TimeoutMilliseconds { get; set; } = -1;

		/// <summary>
		/// If true, arrays will be transfered between C# and Python using the binary .npy format. These are not readable by
		/// humans, but are more efficient.
		/// If false (default), text files (.txt extension) will be used instead, where each array entry is separated by a single 
		/// whitespace char and each row (for 2D arrays) by a newline char. These are readable by humans, but less efficient.
		/// </summary>
		public bool UseBinaryIOFilesForArrays
		{
			get => arrayIO is ArrayBinaryFileIO;
			set
			{
				if (value)
				{
					arrayIO = new ArrayBinaryFileIO();
				}
				else
				{
					arrayIO = new ArrayTextFileIO(' ');
				}
			}
		}

		public bool UseSolutionDifferenceFromPreviousStep { get; set; } = true;

		public bool WriteTrainReportToConsole {  get; set; } = false;

		public bool WritePredictReportsToConsole {  get; set; } = false;

		public bool MustSaveSolution(int timeStep) => true;

		public void SetPythonCodePaths(string pythonInterpreter, string trainScript, string predictScript)
		{
			//TODO: Check if they are valid. Perhaps the scripts can be located from outside the PyCharm directory
			this.pythonInterpreter = pythonInterpreter;
			this.trainScript = trainScript;
			this.predictScript = predictScript;
		}

		public double[] Predict(int timeStep, double[] parameters)
		{
			var watch = new Stopwatch();
			var durations = new PythonCallDurations();

			// Prepare arrays and normalize
			watch.Start();
			float[] inputPy = ArrayTypeUtilities.PrependAndConvertToFloat(timeStep, parameters);
			NormalizationOfParameters.Normalize(inputPy);
			var outputPy = new float[caeFfnnArch.NumDofs];
			watch.Stop();
			durations.DataArraysPreparation += watch.ElapsedMilliseconds;

			// Determine IO files
			watch.Restart();
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PyPredictSettings(workDirectory, extension, pythonModelID, guid);
			settingsFile.Float64 = this.float64;
			var resultsFile = new Py2CsResults(workDirectory, guid);
			var logFile = new Py2CsLog(workDirectory, guid);
			string processArgs = $"{predictScript} {settingsFile.Path} {resultsFile.Path} {logFile.Path}";
			watch.Stop();
			durations.SetupWork += watch.ElapsedMilliseconds;

			try
			{
				// Write input files to filesystem
				watch.Restart();
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				logFile.WriteToFileSystem();
				arrayIO.WriteArray1DToFile(inputPy, settingsFile.ModelParamsPath);
				watch.Stop();
				durations.IO += watch.ElapsedMilliseconds;

				// Call script
				watch.Restart();
				CallPythonScript(processArgs, logFile.Path);
				resultsFile.ReadFromFile();
				watch.Stop();
				durations.Include(watch.ElapsedMilliseconds, resultsFile.Actual, resultsFile.Setup, resultsFile.IO);

				// Read output files from filesystem
				watch.Restart();
				arrayIO.ReadArray1DFromFile(outputPy, settingsFile.SolutionVectorPath);
				watch.Stop();
				durations.IO += watch.ElapsedMilliseconds;

				// Denormalize
				watch.Restart();
				NormalizationOfSolutions.Denormalize(outputPy);
				double[] output = ArrayTypeUtilities.ConvertToDouble(outputPy);
				if (UseSolutionDifferenceFromPreviousStep)
				{
					// In this case, the surrogate returns du[t] = u[t] - u[t-1]
					// If t = 0, u[0] = du[0]
					if (timeStep > 1)
					{
						Vector uPrevious = solutionDb.GetCurrentSolution();
						output.AddIntoThis(uPrevious.RawData);
					}
				}
				watch.Stop();
				durations.DataArraysPreparation += watch.ElapsedMilliseconds;

				if (WritePredictReportsToConsole)
				{
					Console.WriteLine(durations.Report());
				}
				return output;
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(logFile.Path);
					File.Delete(settingsFile.ModelParamsPath);
					File.Delete(settingsFile.SolutionVectorPath);
				}
			}
		}

		public void Train(SolutionDatabaseDynamic solutionDb)
		{
			this.solutionDb = solutionDb;
			var watch = new Stopwatch();
			var durations = new PythonCallDurations();

			// Create datasets and normalize
			watch.Start();
			if (UseSolutionDifferenceFromPreviousStep)
			{
				solutionDb.SubtractSolutionOfPreviousTimestep(); //This will mess up POD if called before it
			}
			float[,] allParams = solutionDb.ToFloatArray2DAllParametersAndTimestepsAsRows(true);
			float[,] allSolutions = solutionDb.ToFloatArray2DAllSolutionsAsRows(true);

			NormalizationOfSolutions.InitializeAndApply(allSolutions);
			NormalizationOfParameters.InitializeAndApply(allParams);
			Splitter.SetupSplittingRules(allSolutions.GetLength(0));
			(float[,] trainSolutions, float[,] testSolutions, _) = Splitter.SplitDataset(allSolutions);
			(float[,] trainParams, float[,] testParams, _) = Splitter.SplitDataset(allParams);
			watch.Stop();
			durations.DataArraysPreparation += watch.ElapsedMilliseconds;

			// Determine IO files
			watch.Restart();
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PyTrainingSettings(caeFfnnArch, workDirectory, extension, pythonModelID, guid);
			settingsFile.Float64 = this.float64;
			settingsFile.TensorFlowSeed = this.TensorFlowSeed;
			var resultsFile = new Py2CsResults(workDirectory, guid);
			var logFile = new Py2CsLog(workDirectory, guid);
			string processArgs = $"{trainScript} {settingsFile.Path} {resultsFile.Path} {logFile.Path}";
			watch.Stop();
			durations.SetupWork += watch.ElapsedMilliseconds;
			
			try
			{
				// Write the files to filesystem
				watch.Restart();
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				logFile.WriteToFileSystem();
				arrayIO.WriteArray2DToFile(trainSolutions, settingsFile.TrainSolutionVectorsPath);
				arrayIO.WriteArray2DToFile(trainParams, settingsFile.TrainModelParamsPath);
				watch.Stop();
				durations.IO += watch.ElapsedMilliseconds;

				// Call script
				watch.Restart();
				CallPythonScript(processArgs, logFile.Path);
				resultsFile.ReadFromFile();
				watch.Stop();
				durations.Include(watch.ElapsedMilliseconds, resultsFile.Actual, resultsFile.Setup, resultsFile.IO);

				if (WriteTrainReportToConsole)
				{
					Console.WriteLine(durations.Report());
				}
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(logFile.Path);
					File.Delete(settingsFile.TrainSolutionVectorsPath);
					File.Delete(settingsFile.TrainModelParamsPath);
				}
			}
		}

		private void CallPythonScript(string processArgs, string pathLog)
		{
			var startInfo = new ProcessStartInfo(pythonInterpreter);
			startInfo.FileName = pythonInterpreter;
			startInfo.Arguments = processArgs;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			int exitCode = -1;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(TimeoutMilliseconds);
				exitCode = process.ExitCode;
			}

			if (exitCode != 0)
			{
				if (exitCode == 100)
				{
					// The results file will be overwritten with the error message
					using (var reader = new StreamReader(pathLog))
					{
						var pythonErrorMsg = reader.ReadToEnd();
						var csharpErrorMsg = new StringBuilder();
						csharpErrorMsg.AppendLine($"Python script terminated with errors:");
						csharpErrorMsg.AppendLine($"**** Start of Python error message ***");
						csharpErrorMsg.AppendLine(pythonErrorMsg);
						csharpErrorMsg.AppendLine($"**** End of Python error message ***");
						throw new Exception(csharpErrorMsg.ToString());
					}
				}
				else
				{
					throw new Exception(
						$"Python script exited with code {exitCode}, instead of 0 (successful) or 100 (handled error).");
				}
			}
		}

		private class Cs2PyTrainingSettings : InteropTempFile
		{
			public Cs2PyTrainingSettings(CaeFfnnArchitecture descr, string workDirectory, string arrayExtension, 
				int modelID, Guid guid)
				: base(workDirectory, "_cs2py_settings.json", guid)
			{
				ModelArchitecture = descr;
				TrainModelParamsPath = tempFilePrefix + "_train_model_params" + arrayExtension;
				TrainSolutionVectorsPath = tempFilePrefix + "_train_solution_vectors" + arrayExtension;
				//TestModelParamsPath = "";
				//TestSolutionVectorsPath = "";
				//ModelEncoderPath = $"{workDirectory}\\model_encoder_{modelID}.keras";
				ModelDecoderPath = $"{workDirectory}\\model_decoder_{modelID}.keras";
				ModelFfnnPath = $"{workDirectory}\\model_ffnn_{modelID}.keras";
			}

			public bool Float64 { get; set; } = false;

			public int TensorFlowSeed { get; set; } = -1;

			public string TrainModelParamsPath { get; }

			public string TrainSolutionVectorsPath { get; }

			//public string TestModelParamsPath { get; }

			//public string TestSolutionVectorsPath { get; }

			//public string ModelEncoderPath { get; }

			public string ModelDecoderPath { get; }

			public string ModelFfnnPath { get; }

			public CaeFfnnArchitecture ModelArchitecture { get; }
		}

		private class Cs2PyPredictSettings : InteropTempFile
		{
			public Cs2PyPredictSettings(string workDirectory, string arrayExtension, int modelID, Guid guid)
				: base(workDirectory, "_cs2py_settings.json", guid)
			{
				ModelParamsPath = tempFilePrefix + "_model_params" + arrayExtension;
				SolutionVectorPath = tempFilePrefix + "_solution_vector" + arrayExtension;
				ModelDecoderPath = $"{workDirectory}\\model_decoder_{modelID}.keras";
				ModelFfnnPath = $"{workDirectory}\\model_ffnn_{modelID}.keras";
			}

			public bool Float64 { get; set; } = false;

			public string ModelParamsPath { get; }

			public string SolutionVectorPath { get; }

			public string ModelDecoderPath { get; }

			public string ModelFfnnPath { get; }
		}

		private class Py2CsLog : InteropTempFile
		{
			public Py2CsLog(string workDirectory, Guid guid) : base(workDirectory, "_py2cs_log.json", guid)
			{
			}

			//public string Message { get; set; } = ""; //TODO: Error messages should go in this property
		}

		/// <summary>
		/// Communicates time measurements
		/// </summary>
		private class Py2CsResults : InteropTempFile
		{
			public Py2CsResults(string workDirectory, Guid guid) : base(workDirectory, "_py2cs_results.json", guid)
			{
			}

			public int IO { get; set; } = -1;

			public int Setup { get; set; } = -1;

			public int Actual { get; set; } = -1;

			public void CopyFrom(Py2CsResults other)
			{
				this.IO = other.IO;
				this.Setup = other.Setup;
				this.Actual = other.Actual;
			}

			public void ReadFromFile()
			{
				using StreamReader reader = new(Path);
				var json = reader.ReadToEnd();
				Py2CsResults result = JsonConvert.DeserializeObject<Py2CsResults>(json);
				CopyFrom(result);
			}
		}
	}
}
