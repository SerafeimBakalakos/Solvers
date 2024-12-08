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
		private readonly string workDirectory;
		private readonly int pythonModelID;
		private readonly bool timestepAsModelParam;
		private readonly CaeFfnnArchitecture caeFfnnArch;

		private IArrayFileIO arrayIO = new ArrayBinaryFileIO();
		private SolutionDatabaseDynamic solutionDb;

		private string pythonInterpreter = null;
		private string trainScript = null;
		private string predictScript = null;
		private string predictHistoryScript = null;

		private Dictionary<int, Vector> batchInitialGuessesForHistory { get; set; }
		private int batchSize;

		public CaeFfnnSurrogateDynamicPythonTF(CaeFfnnArchitecture caeFfnnArchitecture, string workDirectory, int pythonModelID,
			bool timestepAsModelParam)
		{
			this.caeFfnnArch = caeFfnnArchitecture;
			this.workDirectory = workDirectory;
			this.pythonModelID = pythonModelID;
			this.timestepAsModelParam = timestepAsModelParam;
			Splitter = new DatasetSplitter();
			Splitter.MinTestSetPercentage = 0.2;
			Splitter.MinValidationSetPercentage = 0.0;
			Splitter.SetOrderToContiguous(DataSubsetType.Training, DataSubsetType.Test);
		}

		public bool BatchTimeHistoryPredictions { get; set; } = false;

		/// <summary>
		/// True (default) to delete any files created by this class. False to retain the files for manual inspection.
		/// </summary>
		public bool CleanupIOFiles { get; set; } = true;

		public bool Float64 { get; set; } = false;

		public INormalizationStrategy NormalizationOfParameters { get; set; } = new MinMaxNormalization();

		public INormalizationStrategy NormalizationOfSolutions { get; set; } = new NullNormalization();

		public bool ReadMLNetworksFromFilesWithoutTraining { get; set; } = false;

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

		public bool WriteTrainReportToConsole { get; set; } = false;

		public bool WritePredictReportsToConsole { get; set; } = false;

		public bool MustSaveSolution(int timeStep) => true;

		public void SetPythonCodePaths(string pythonInterpreter, string trainScript, string predictScript)
		{
			//TODO: Check if they are valid. Perhaps the scripts can be located from outside the PyCharm directory
			this.pythonInterpreter = pythonInterpreter;
			this.trainScript = trainScript;
			this.predictScript = predictScript;

			int fileExtPos = predictScript.LastIndexOf(".");
			if (fileExtPos >= 0)
			{
				predictHistoryScript = predictScript.Substring(0, fileExtPos) + "_history.py";
			}
		}


		private static void WriteArrayToFile(string path, float[] array, string separator = "\n")
		{
			using (var f = File.Open(path, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					writer.Write(array[0]);
					for (int i = 1; i < array.Length; i++)
					{
						writer.Write(separator);
						writer.Write(array[i].ToString("G"));
					}
				}
			}
		}

		public double[] Predict(int timeStep, double[] parameters)
		{
			if (BatchTimeHistoryPredictions)
			{
				if (timeStep == 0)
				{
					PredictHistory(parameters);
				}
				return batchInitialGuessesForHistory[timeStep].RawData;
			}

			var watch = new Stopwatch();
			var durations = new PythonCallDurations();

			// Prepare arrays and normalize
			watch.Start();
			float[] inputPy = timestepAsModelParam ? ArrayTypeUtilities.PrependAndConvertToFloat(timeStep, parameters)
				: ArrayTypeUtilities.ConvertToFloat(parameters);

			#region debug
			//WriteArrayToFile(Path.Combine(workDirectory, "input_before_normalization_cs.txt"), inputPy);
			#endregion

			NormalizationOfParameters.Normalize(inputPy);
			#region debug
			//WriteArrayToFile(Path.Combine(workDirectory, "input_after_normalization_cs.txt"), inputPy);
			#endregion
			var outputPy = new float[caeFfnnArch.NumDofs];
			watch.Stop();
			durations.DataArraysPreparation += watch.ElapsedMilliseconds;

			// Determine IO files
			watch.Restart();
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PyPredictSettings(workDirectory, extension, pythonModelID, guid);
			settingsFile.Float64 = this.Float64;
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
				#region debug
				//WriteArrayToFile(Path.Combine(workDirectory, "output_before_denormalization_cs.txt"), outputPy);
				#endregion
				watch.Stop();
				durations.IO += watch.ElapsedMilliseconds;

				// Denormalize and process python surrogate's output
				watch.Restart();
				NormalizationOfSolutions.Denormalize(outputPy);
				#region debug
				//WriteArrayToFile(Path.Combine(workDirectory, "output_after_denormalization_cs.txt"), outputPy);
				#endregion
				var prediction = Vector.CreateFromArray(ArrayTypeUtilities.ConvertToDouble(outputPy));
				if (UseSolutionDifferenceFromPreviousStep)
				{
					// In this case, the surrogate returns du[t] = u[t] - u[t-1]
					// If t = 0, u[0] = du[0]
					if (timeStep > 1)
					{
						Vector uPrevious = solutionDb.GetCurrentSolution();
						prediction.AddIntoThis(uPrevious);
					}
				}
				watch.Stop();
				durations.DataArraysPreparation += watch.ElapsedMilliseconds;

				// Finalize
				if (WritePredictReportsToConsole)
				{
					Console.WriteLine(durations.Report());
				}

				return prediction.RawData;
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

		private void PredictHistory(double[] parameters)
		{
			var watch = new Stopwatch();
			var durations = new PythonCallDurations();

			// Prepare arrays and normalize
			watch.Start();
			int numParams = timestepAsModelParam ? parameters.Length + 1 : parameters.Length;
			float[,] inputArraysPy = new float[batchSize, numParams];
			for (int t = 0; t < batchSize; t++)
			{
				float[] inputPy = timestepAsModelParam ? ArrayTypeUtilities.PrependAndConvertToFloat(t, parameters)
					: ArrayTypeUtilities.ConvertToFloat(parameters);
				NormalizationOfParameters.Normalize(inputPy);
				SetRow(inputArraysPy, t, inputPy);
			}
			
			float[,] outputArraysPy = null;
			watch.Stop();
			durations.DataArraysPreparation += watch.ElapsedMilliseconds;

			// Determine IO files
			watch.Restart();
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PyPredictSettings(workDirectory, extension, pythonModelID, guid);
			settingsFile.Float64 = this.Float64;
			var resultsFile = new Py2CsResults(workDirectory, guid);
			var logFile = new Py2CsLog(workDirectory, guid);
			string processArgs = $"{predictHistoryScript} {settingsFile.Path} {resultsFile.Path} {logFile.Path}";
			watch.Stop();
			durations.SetupWork += watch.ElapsedMilliseconds;

			try
			{
				// Write input files to filesystem
				watch.Restart();
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				logFile.WriteToFileSystem();
				arrayIO.WriteArray2DToFile(inputArraysPy, settingsFile.ModelParamsPath);
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
				outputArraysPy = arrayIO.ReadArray2DFromFile(settingsFile.SolutionVectorPath);

				#region debug
				//var outputArrayPy1D = new float[300];
				//arrayIO.ReadArray1DFromFile(outputArrayPy1D, settingsFile.SolutionVectorPath);
				//outputArraysPy = Array1DTo2D(outputArrayPy1D);
				//WriteArrayToFile(Path.Combine(workDirectory, "output_before_denormalization_cs.txt"), outputPy);
				#endregion
				watch.Stop();
				durations.IO += watch.ElapsedMilliseconds;

				// Denormalize and store for later calls
				watch.Restart();
				batchInitialGuessesForHistory.Clear();
				for (int t = 0; t < batchSize; t++)
				{
					float[] singleOutputVector = GetRow(outputArraysPy, t);
					NormalizationOfSolutions.Denormalize(singleOutputVector);
					var prediction = Vector.CreateFromArray(ArrayTypeUtilities.ConvertToDouble(singleOutputVector));
					batchInitialGuessesForHistory[t] = prediction;
				}
				
				if (UseSolutionDifferenceFromPreviousStep)
				{
					throw new NotImplementedException("Must add to the predictions obtained");
				}
				watch.Stop();
				durations.DataArraysPreparation += watch.ElapsedMilliseconds;

				if (WritePredictReportsToConsole)
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

			if (BatchTimeHistoryPredictions)
			{
				batchSize = solutionDb.CountTimeSteps();
				batchInitialGuessesForHistory = new Dictionary<int, Vector>();
			}

			// Create datasets and normalize
			watch.Start();
			if (UseSolutionDifferenceFromPreviousStep)
			{
				solutionDb.SubtractSolutionOfPreviousTimestep(); //This will mess up POD if called before it
			}
			float[,] allParams = solutionDb.ToFloatArray2DAllParametersAndTimestepsAsRows(timestepAsModelParam, true);
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
			settingsFile.Float64 = this.Float64;
			settingsFile.TensorFlowSeed = this.TensorFlowSeed;
			var resultsFile = new Py2CsResults(workDirectory, guid);
			var logFile = new Py2CsLog(workDirectory, guid);
			string processArgs = $"{trainScript} {settingsFile.Path} {resultsFile.Path} {logFile.Path}";
			watch.Stop();
			durations.SetupWork += watch.ElapsedMilliseconds;

			if (ReadMLNetworksFromFilesWithoutTraining == true)
			{
				//string decoderPath = $"{workDirectory}\\model_decoder_{pythonModelID}.keras";
				//string ffnnPath = $"{workDirectory}\\model_ffnn_{pythonModelID}.keras";
				string decoderPath = settingsFile.ModelDecoderPath;
				string ffnnPath = settingsFile.ModelFfnnPath;
				if (!File.Exists(decoderPath))
				{
					throw new InvalidOperationException(
						"Training is set to be skipped, but there is no convolutional decoder network at path = " + decoderPath);
				}
				else if (!File.Exists(ffnnPath))
				{
					throw new InvalidOperationException("Training is set to be skipped, but there is no FFNN network at path = " + ffnnPath);
				}
				else
				{
					Console.WriteLine("Skipped training step. The ML surrogate networks will be read from filesystem instead.");
					return;
				}
			}

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

		private Vector SolveExactly()
		{
			throw new NotImplementedException();
		}

		private float[] GetRow(float[,] array2D, int rowIdx)
		{
			int numCols = array2D.GetLength(1);
			var result = new float[numCols];
			for (int j = 0; j < numCols; j++)
			{
				result[j] = array2D[rowIdx, j];
			}
			return result;
		}

		private void SetRow(float[,] array2D, int rowIdx, float[] rowValues)
		{
			int numCols = array2D.GetLength(1);
			Debug.Assert(rowValues.Length == numCols);
			for (int j = 0; j < numCols; j++)
			{
				array2D[rowIdx, j] = rowValues[j];
			}
		}

		#region debug
		//private static T[,] Array1DTo2D<T>(T[] array1D)
		//{
		//	var result = new T[1, array1D.Length];
		//	for (int i = 0; i < array1D.Length; i++)
		//	{
		//		result[0, i] = array1D[i];
		//	}
		//	return result;
		//}
		#endregion

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
