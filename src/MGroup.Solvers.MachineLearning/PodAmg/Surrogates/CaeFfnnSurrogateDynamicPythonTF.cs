namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;

	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MachineLearning.TensorFlow.KerasLayers;
	using MGroup.MachineLearning.Utilities;
	using MGroup.Solvers.MachineLearning.MLExtensions;

	public class CaeFfnnSurrogateDynamicPythonTF
	{
		private readonly string workDirectory;
		private readonly int pythonModelID;
		private readonly CaeFfnnArchitecture caeFfnnArch;

		private IArrayFileIO arrayIO = new ArrayBinaryFileIO();

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

		public bool Float64 { get; set; } = false;

		public int TensorFlowSeed { get; set; } = -1;

		public DatasetSplitter Splitter { get; set; }

		/// <summary>
		/// True (default) to delete any files created by this class. False to retain the files for manual inspection.
		/// </summary>
		public bool CleanupIOFiles { get; set; } = true;

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

		public void SetPythonCodePaths(string pythonInterpreter, string trainScript, string predictScript)
		{
			//TODO: Check if they are valid. Perhaps the scripts can be located from outside the PyCharm directory
			this.pythonInterpreter = pythonInterpreter;
			this.trainScript = trainScript;
			this.predictScript = predictScript;
		}

		public double[] Predict(int timeStep, double[] parameters)
		{
			double[] input = Prepend(timeStep, parameters);
			var output = new double[caeFfnnArch.NumDofs];

			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			var settingsFile = new Cs2PyPredictSettings(workDirectory, extension, pythonModelID);
			settingsFile.Float64 = this.Float64;
			var resultsFile = new Py2CsResults(workDirectory);
			string processArgs = $"{predictScript} {settingsFile.Path} {resultsFile.Path}";
			try
			{
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				arrayIO.WriteArray1DToFile(input, settingsFile.ModelParamsPath);
				CallPythonScript(processArgs, resultsFile.Path);
				arrayIO.ReadArray1DFromFile(output, settingsFile.SolutionVectorPath);
				return output;
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(settingsFile.ModelParamsPath);
					File.Delete(settingsFile.SolutionVectorPath);
				}
			}
		}

		public void Train(SolutionDatabaseDynamic solutionDb)
		{
			// Create datasets
			double[,] allSolutions = solutionDb.ToArray2DAllSolutionsAsRows(true);
			double[,] allParams = solutionDb.ToArray2DAllParametersAndTimestepsAsRows(true);
			Splitter.SetupSplittingRules(allSolutions.GetLength(0));
			(double[,] trainSolutions, double[,] testSolutions, _) = Splitter.SplitDataset(allSolutions);
			(double[,] trainParams, double[,] testParams, _) = Splitter.SplitDataset(allParams);

			// Determine IO files
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			var settingsFile = new Cs2PyTrainingSettings(caeFfnnArch, workDirectory, extension, pythonModelID);
			settingsFile.Float64 = this.Float64;
			settingsFile.TensorFlowSeed = this.TensorFlowSeed;
			var resultsFile = new Py2CsResults(workDirectory);
			string processArgs = $"{trainScript} {settingsFile.Path} {resultsFile.Path}";
			try
			{
				// Write the files to filesystem
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				arrayIO.WriteArray2DToFile(trainSolutions, settingsFile.TrainSolutionVectorsPath);
				arrayIO.WriteArray2DToFile(trainParams, settingsFile.TrainModelParamsPath);
				CallPythonScript(processArgs, resultsFile.Path);
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(settingsFile.TrainSolutionVectorsPath);
					File.Delete(settingsFile.TrainModelParamsPath);
				}
			}
		}

		private void CallPythonScript(string processArgs, string pathResults)
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
					using (var reader = new StreamReader(pathResults))
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

		private double[] Prepend(double newValue, double[] oldArray)
		{
			var result = new double[oldArray.Length + 1];
			result[0] = newValue;
			Array.Copy(oldArray, 0, result, 1, oldArray.Length);
			return result;
		}

		private class Cs2PyTrainingSettings : InteropTempFile
		{
			public Cs2PyTrainingSettings(CaeFfnnArchitecture descr, string workDirectory, string arrayExtension, int modelID)
				: base(workDirectory, "_cs2py_settings.json")
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
			public Cs2PyPredictSettings(string workDirectory, string arrayExtension, int modelID)
				: base(workDirectory, "_cs2py_settings.json")
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

		private class Py2CsResults : InteropTempFile
		{
			public Py2CsResults(string workDirectory) : base(workDirectory, "_py2cs_results.json")
			{
			}

			//public string Message { get; set; } = ""; //TODO: Error messages should go in this property
		}
	}
}
