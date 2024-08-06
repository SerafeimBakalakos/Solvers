namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using Google.Protobuf;
	using Newtonsoft.Json;

	public class PythonSurrogate
	{
		private readonly int modelID;
		private readonly string workDir;
		private readonly string pythonInterpreter;
		private readonly string trainScript;
		private readonly string predictScript;
		private readonly int sizeInput;
		private readonly int sizeOutput;

		private IArrayFileIO arrayIO = new ArrayTextFileIO(' ');

		public PythonSurrogate(int modelID, string workDir, string pythonInterpreter, string trainScript, string predictScript,
			int sizeInput, int sizeOutput)
		{
			this.modelID = modelID;
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.trainScript = trainScript;
			this.predictScript = predictScript;
			this.sizeInput = sizeInput;
			this.sizeOutput = sizeOutput;
		}

		/// <summary>
		/// True (default) to delete any files created by this class. False to retain the files for manual inspection.
		/// </summary>
		public bool CleanupIOFiles { get; set; } = true;

		/// <summary>
		/// Sets a seed value for all random number generations used by TensorFlow in Python scipt. No seed will be 
		/// used, if <see cref="TensorFlowSeed"/> == -1 (default). Setting a seed is useful to obtain reproducible results.
		/// </summary>
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

		public double[] CallPredictScript(double[] input)
		{
			CheckPredictionData(input);
			var output = new double[sizeOutput];
			CallPredictScript(true,
				(pathInput) => arrayIO.WriteArray1DToFile(input, pathInput),
				(pathOutput) => arrayIO.ReadArray1DFromFile(output, pathOutput)
			);
			return output;
		}

		public float[] CallPredictScript(float[] input)
		{
			CheckPredictionData(input);
			var output = new float[sizeOutput];
			CallPredictScript(false,
				(pathInput) => arrayIO.WriteArray1DToFile(input, pathInput),
				(pathOutput) => arrayIO.ReadArray1DFromFile(output, pathOutput)
			);
			return output;
		}

		public void CallTrainScript(double[,] features, double[,] labels)
		{
			CheckTrainData(features, labels);
			CallTrainScript(true, (string pathFeatures, string pathLabels) =>
			{
				arrayIO.WriteArray2DToFile(features, pathFeatures);
				arrayIO.WriteArray2DToFile(labels, pathLabels);
			});
		}

		public void CallTrainScript(float[,] features, float[,] labels)
		{
			CheckTrainData(features, labels);
			CallTrainScript(false, (string pathFeatures, string pathLabels) =>
			{
				arrayIO.WriteArray2DToFile(features, pathFeatures);
				arrayIO.WriteArray2DToFile(labels, pathLabels);
			});
		}

		private void CallPredictScript(bool doublePrecision, Action<string> writeInputToFile, Action<string> readOutputFromFile)
		{
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PySettings(workDir, extension, modelID, guid);
			settingsFile.Float64 = doublePrecision;
			var resultsFile = new Py2CsResults(workDir, guid);
			string processArgs = $"{predictScript} {settingsFile.Path} {resultsFile.Path}";
			try
			{
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				writeInputToFile(settingsFile.FeaturesPath);
				CallPythonScript(processArgs, resultsFile.Path);
				readOutputFromFile(settingsFile.LabelsPath);
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(settingsFile.FeaturesPath);
					File.Delete(settingsFile.LabelsPath);
				}
			}
		}

		private void CallTrainScript(bool doublePrecision, Action<string, string> writeFeaturesAndLabelsToFiles)
		{
			string extension = (arrayIO is ArrayBinaryFileIO) ? ".npy" : ".txt";
			Guid guid = Guid.NewGuid();
			var settingsFile = new Cs2PyTrainingSettings(workDir, extension, modelID, guid);
			settingsFile.Float64 = doublePrecision;
			settingsFile.TensorFlowSeed = this.TensorFlowSeed;
			var resultsFile = new Py2CsResults(workDir, guid);
			string processArgs = $"{trainScript} {settingsFile.Path} {resultsFile.Path}";
			try
			{
				settingsFile.WriteToFileSystem();
				resultsFile.WriteToFileSystem();
				writeFeaturesAndLabelsToFiles(settingsFile.FeaturesPath, settingsFile.LabelsPath);
				CallPythonScript(processArgs, resultsFile.Path);
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile.Path);
					File.Delete(resultsFile.Path);
					File.Delete(settingsFile.FeaturesPath);
					File.Delete(settingsFile.LabelsPath);
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

		[Conditional("DEBUG")]
		private void CheckTrainData<T>(T[,] features, T[,] labels)
		{
			if (features.GetLength(0) != labels.GetLength(0))
			{
				throw new ArgumentException($"Features array contains {features.GetLength(0)} samples," +
					$" but labels array contains {labels.GetLength(0)} samples");
			}
			if (features.GetLength(1) != sizeInput)
			{
				throw new ArgumentException($"Features array contains {features.GetLength(1)} different features," +
					$" but the input size is {sizeInput}");
			}
			if (labels.GetLength(1) != sizeOutput)
			{
				throw new ArgumentException($"Labels array contains {features.GetLength(1)} different labels," +
					$" but the output size is {sizeOutput}");
			}
		}

		[Conditional("DEBUG")]
		private void CheckPredictionData<T>(T[] input)
		{
			if (input.Length != sizeInput)
			{
				throw new ArgumentException($"The input size is {sizeInput}, but {input.Length} entries were provided.");
			}
		}

		private string GetTempFilePathPrefix()
		{
			var time = DateTime.Now;
			string prefix = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";
			return $"{workDir}\\{prefix}";
		}

		private class Cs2PySettings : InteropTempFile
		{
			public Cs2PySettings(string workDirectory, string arrayExtension, int modelID, Guid guid) 
				: base(workDirectory, "_cs2py_settings.json", guid)
			{
				FeaturesPath = tempFilePrefix + "_features" + arrayExtension;
				LabelsPath = tempFilePrefix + "_labels" + arrayExtension;
				ModelPath = $"{workDirectory}\\model_{modelID}.keras";
			}

			public string FeaturesPath { get; }

			public string LabelsPath { get; }

			public string ModelPath { get; }

			/// <summary>
			/// TensorFlow will use: double precision if true, else single precision.
			/// </summary>
			public bool Float64 { get; set; } = false;
		}

		private class Cs2PyTrainingSettings : Cs2PySettings
		{
			public Cs2PyTrainingSettings(string workDirectory, string arrayExtension, int modelID, Guid guid) 
				: base(workDirectory, arrayExtension, modelID, guid)
			{
			}

			/// <summary>
			/// If the value is not -1, TensorFlow will use this seed for all RNG (useful to reproduce results).
			/// </summary>
			public int TensorFlowSeed { get; set; } = -1;
		}

		private class Py2CsResults : InteropTempFile
		{
			public Py2CsResults(string workDirectory, Guid guid) : base(workDirectory, "_py2cs_results.json", guid)
			{
			}

			//public string Message { get; set; } = ""; //TODO: Error messages should go in this property
		}

		private class SettingsOLD
		{
			/// <summary>
			/// TensorFlow will use: double precision if true, else single precision.
			/// </summary>
			public bool Float64 { get; set; } = false;

			/// <summary>
			/// If the value is not -1, TensorFlow will use this seed for all RNG (useful to reproduce results).
			/// </summary>
			public int Seed { get; set; } = -1;
		}
	}
}
