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
		private readonly IReadOnlyList<string> inputsSerializedNames = new List<string>();


		private Dictionary<string, string> inputsSerializedValues = new Dictionary<string, string>();
		private Dictionary<string, double[]> inputsArraysValuesFloat64 = new Dictionary<string, double[]>();
		private Dictionary<string, double[]> outputsArraysValuesFloat64 = new Dictionary<string, double[]>();

		internal PythonCall(string workDirectory, string pythonInterpreterPath, string pythonScriptPath,
			bool cleanupIOFiles, int timeoutMilliseconds, string inputsSerializedTemplate, string inputsArraysTemplate)
		{
			this.workDirectory = workDirectory;
			this.pythonInterpreterPath = pythonInterpreterPath;
			this.pythonScriptPath = pythonScriptPath;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;
			this.inputsSerializedTemplate = inputsSerializedTemplate;
			this.inputsArraysTemplate = inputsArraysTemplate;
		}

		public void PassSmallInput(string name, string value)
		{
			inputsSerializedValues[name] = value;
		}

		public void PassArrayInput(string name, double[] array)
		{
			inputsArraysValuesFloat64[name] = array;
		}

		public double[] GetArrayOutput(string name)
		{
			return outputsArraysValuesFloat64[name];
		}

		public PythonCallDurations LastCallDurations { get; private set; } = new PythonCallDurations();

		public void Execute()
		{
			var watch = new Stopwatch();
			watch.Restart();
			LastCallDurations = new PythonCallDurations();

			DirectoryInfo tempDirectory = null;
			var paths = new PythonCallPaths(workDirectory);
			string processArgs = $"{pythonScriptPath} {paths.InputsSerialized} {paths.InputsArrays} {paths.OutputsSerialized} {paths.OutputsSerialized} {paths.LogPerformance} {paths.LogErrors}";
			watch.Stop();
			LastCallDurations.PythonSetupWork += watch.ElapsedMilliseconds;

			try
			{
				// Write input files to filesystem
				watch.Restart();
				tempDirectory = Directory.CreateDirectory(paths.TempSubdirectoryFullPath);
				WriteInputsSerializedToFiles(paths);
				WriteInputsArraysToFiles(paths);
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
				DeleteInputs();
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
				serializedInputs[i] = inputsSerializedValues[inputsSerializedNames[i]];
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
			using (StreamWriter writer = File.CreateText(paths.InputsSerialized))
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

		}

		private (long pythonCommunicationWork, long pythonSetupWork) ReadPythonPerformance(PythonCallPaths paths)
		{
			return (0, 0);
		}

		private void DeleteInputs()
		{
			inputsSerializedValues.Clear();
			inputsArraysValuesFloat64.Clear();
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
