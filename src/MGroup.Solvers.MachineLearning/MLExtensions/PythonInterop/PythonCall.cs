using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using NumSharp;

namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
    public class PythonCall
    {
		private readonly string workDirectory;
		private readonly string pythonInterpreterPath;
		private readonly string pythonScriptPath;
		private readonly bool cleanupIOFiles;
		private readonly int timeoutMilliseconds;
		private readonly string inputsSerializedTemplate;
		private readonly string inputsArraysTemplate;
		private readonly string outputsArraysTemplate;
		private readonly IReadOnlyList<string> inputsSerializedOrdered;
		private readonly ISet<string> inputsSerializedNames;
		private readonly ISet<string> inputsArraysNames;
		private readonly ISet<string> outputsArraysNames;

		private Dictionary<string, string> inputsSerializedValues = new Dictionary<string, string>();
		private Dictionary<string, double[]> inputsArraysValuesFloat64 = new Dictionary<string, double[]>();
		private Dictionary<string, double[]> outputsArraysValuesFloat64 = new Dictionary<string, double[]>();

		internal PythonCall(string workDirectory, string pythonInterpreterPath, string pythonScriptPath,
			bool cleanupIOFiles, int timeoutMilliseconds,
			ISet<string> inputsSerializedNames, IReadOnlyList<string> inputsSerializedOrdered, string inputsSerializedTemplate,
			ISet<string> inputsArraysNames, string inputsArraysTemplate,
			ISet<string> outputsArraysNames, string outputsArraysTemplate)
		{
			this.workDirectory = workDirectory;
			this.pythonInterpreterPath = pythonInterpreterPath;
			this.pythonScriptPath = pythonScriptPath;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;

			this.inputsSerializedNames = inputsSerializedNames;
			this.inputsSerializedTemplate = inputsSerializedTemplate;
			this.inputsSerializedOrdered = inputsSerializedOrdered;
			this.inputsArraysNames = inputsArraysNames;
			this.inputsArraysTemplate = inputsArraysTemplate;
			this.outputsArraysNames = outputsArraysNames;
			this.outputsArraysTemplate = outputsArraysTemplate;
		}

		public void PassSmallInput(string name, string value)
		{
			if (!inputsSerializedNames.Contains(name))
			{
				throw new ArgumentException($"No input with the name {name} has been defined");
			}
			inputsSerializedValues[name] = value;
		}

		public void PassArrayInput(string name, double[] array)
		{
			if (!inputsArraysNames.Contains(name))
			{
				throw new ArgumentException($"No input array with the name {name} has been defined");
			}
			inputsArraysValuesFloat64[name] = array;
		}

		public double[] GetArrayOutput(string name)
		{
			if (!outputsArraysNames.Contains(name))
			{
				throw new ArgumentException($"No output array with the name {name} has been defined");
			}
			return outputsArraysValuesFloat64[name];
		}

		public PythonCallDurations LastCallDurations { get; private set; } = new PythonCallDurations();

		public void Execute()
		{
			var watch = new Stopwatch();
			watch.Restart();
			LastCallDurations = new PythonCallDurations();
			DeleteStoredOutputValues();

			DirectoryInfo tempDirectory = null;
			var paths = new PythonCallPaths(workDirectory);
			string processArgs = $"{pythonScriptPath} {paths.InputsSerialized} {paths.InputsArrays} {paths.OutputsSerialized} {paths.OutputsArrays} {paths.LogPerformance} {paths.LogErrors}";
			watch.Stop();
			LastCallDurations.PythonSetupWork += watch.ElapsedMilliseconds;

			try
			{
				// Write input files to filesystem
				watch.Restart();
				tempDirectory = Directory.CreateDirectory(paths.TempSubdirectoryFullPath);
				WriteInputsSerializedToFiles(paths);
				WriteInputsArraysToFiles(paths);
				WriteOuputFiles(paths);
				WriteLogFiles(paths);
				watch.Stop();
				LastCallDurations.CommunicationWork += watch.ElapsedMilliseconds;

				// Call script
				watch.Restart();
				RunSystemProcess(processArgs, paths.LogErrors);
				(long pythonCommunicationWork, long pythonSetupWork) = ReadPythonPerformance(paths);
				watch.Stop();
				LastCallDurations.Include(watch.ElapsedMilliseconds, pythonCommunicationWork, pythonSetupWork);

				// Read output files from filesystem
				watch.Restart();
				ReadOutputFiles(paths);
				watch.Stop();
				LastCallDurations.CommunicationWork += watch.ElapsedMilliseconds;
			}
			finally
			{
				// Cleanup
				DeleteStoredInputValues();
				if (cleanupIOFiles)
				{
					if (tempDirectory != null)
					{
						tempDirectory.Delete(recursive: true);
					}
				}
			}
		}

		private void WriteInputsSerializedToFiles(PythonCallPaths paths)
		{
			int numInputs = inputsSerializedNames.Count;
			var serializedInputs = new object[numInputs];
			for (int i = 0; i < numInputs; i++)
			{
				serializedInputs[i] = inputsSerializedValues[inputsSerializedOrdered[i]];
			}

			string fileContent = string.Format(inputsSerializedTemplate, serializedInputs);
			using (StreamWriter writer = File.CreateText(paths.InputsSerialized))
			{
				writer.Write(fileContent);
			}
		}

		private void WriteInputsArraysToFiles(PythonCallPaths paths)
		{
			// Write the arrays to separate paths
			foreach ((string name, double[] array) in inputsArraysValuesFloat64)
			{
				np.Save(array, paths.MakePathForArray(name)); 
			}

			// Write the file that contains the paths
			string fileContent = string.Format(inputsArraysTemplate, paths.TempSubdirectoryNameOnly);
			using (StreamWriter writer = File.CreateText(paths.InputsArrays))
			{
				writer.Write(fileContent);
			}
		}

		private void WriteOuputFiles(PythonCallPaths paths)
		{
			using (StreamWriter writer = File.CreateText(paths.OutputsSerialized))
			{
				writer.Write(' '); // placeholder
			}

			// Write the file that contains the paths
			string fileContent = string.Format(outputsArraysTemplate, paths.TempSubdirectoryNameOnly);
			using (StreamWriter writer = File.CreateText(paths.OutputsArrays))
			{
				writer.Write(fileContent);
			}
		}

		private void WriteLogFiles(PythonCallPaths paths)
		{
			// Performance

			// Errors

		}

		private void ReadOutputFiles(PythonCallPaths paths)
		{
			// Read the arrays from separate paths
			foreach (string name in outputsArraysNames)
			{
				string path = paths.MakePathForArray(name);
				outputsArraysValuesFloat64[name] = np.Load<double[]>(path);
			}
		}

		private (long pythonCommunicationWork, long pythonSetupWork) ReadPythonPerformance(PythonCallPaths paths)
		{
			return (0, 0);
		}

		private void DeleteStoredInputValues()
		{
			inputsSerializedValues.Clear();
			inputsArraysValuesFloat64.Clear();
		}

		private void DeleteStoredOutputValues()
		{
			outputsArraysValuesFloat64.Clear();
		}

		private void RunSystemProcess(string processArgs, string pathLogErrors)
		{
			var startInfo = new ProcessStartInfo(pythonInterpreterPath);
			startInfo.FileName = pythonInterpreterPath;
			startInfo.Arguments = processArgs;
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			int exitCode = -1;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(timeoutMilliseconds);
				exitCode = process.ExitCode;
			}

			if (exitCode != 0)
			{
				if (exitCode == 100)
				{
					// The results file will be overwritten with the error message
					using (var reader = new StreamReader(pathLogErrors))
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
		
	}
}
