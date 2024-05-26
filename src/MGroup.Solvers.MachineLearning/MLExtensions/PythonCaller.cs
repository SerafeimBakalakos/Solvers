namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Xml.Linq;

	using Newtonsoft.Json;

	public class PythonCaller
	{
		private readonly string workDir;
		private readonly string pythonInterpreter;
		private readonly string pythonScript;

		private IArrayFileIO arrayIO = new ArrayTextFileIO(' ');

		public PythonCaller(string workDir, string pythonInterpreter, string pythonScript)
		{
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.pythonScript = pythonScript;
		}

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

		public double[] CallPython(double[] input)
		{
			(string settingsFile, string inputFile, string outputFile) = CreateFilenames();

			try
			{
				WriteSettingsFile(true, settingsFile);
				arrayIO.WriteArray1DToFile(input, inputFile);
				CallPythonScript(settingsFile, inputFile, outputFile);
				var output = new double[input.Length];
				arrayIO.ReadArray1DFromFile(output, outputFile);
				return output;
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile);
					File.Delete(inputFile);
					File.Delete(outputFile);
				}
			}
		}

		public float[] CallPython(float[] input)
		{
			(string settingsFile, string inputFile, string outputFile) = CreateFilenames();

			try
			{
				WriteSettingsFile(false, settingsFile);
				arrayIO.WriteArray1DToFile(input, inputFile);
				CallPythonScript(settingsFile, inputFile, outputFile);
				var output = new float[input.Length];
				arrayIO.ReadArray1DFromFile(output, outputFile);
				return output;
			}
			finally
			{
				// Cleanup
				if (CleanupIOFiles)
				{
					File.Delete(settingsFile);
					File.Delete(inputFile);
					File.Delete(outputFile);
				}
			}
		}

		private void CallPythonScript(string settingsFile, string inputFile, string outputFile)
		{
			var startInfo = new ProcessStartInfo(pythonInterpreter);
			startInfo.FileName = pythonInterpreter;
			startInfo.Arguments = $"{pythonScript} {settingsFile} {inputFile} {outputFile}";
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			//startInfo.RedirectStandardError = true;
			int exitCode = -1;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(TimeoutMilliseconds);
				exitCode = process.ExitCode;
			}

			// The settings file will be overwritten with the error message
			using (var reader = new StreamReader(settingsFile))
			{
				var pythonErrorMsg = reader.ReadToEnd();
				var csharpErrorMsg = new StringBuilder();
				csharpErrorMsg.AppendLine($"Python script exited with code {exitCode}, instead of 0 (successful).");
				csharpErrorMsg.AppendLine($"**** Start of Python error message ***");
				csharpErrorMsg.AppendLine(pythonErrorMsg);
				csharpErrorMsg.AppendLine($"**** End of Python error message ***");
				throw new Exception(csharpErrorMsg.ToString());
			}
		}

		private (string settingsFile, string inputFile, string outputFile) CreateFilenames()
		{
			// Setup IO files
			var time = DateTime.Now;
			string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}{time.Second}";
			//string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";

			string extension = (arrayIO is ArrayBinaryFileIO) ? "npy" : "txt";
			string settingsFile = $"{workDir}\\{filename}_settings.json";
			string inputFile = $"{workDir}\\{filename}_in.{extension}";
			string outputFile = $"{workDir}\\{filename}_out.{extension}";

			return (settingsFile, inputFile, outputFile);
		}

		private void WriteSettingsFile(bool doublePrecision, string path) 
		{
			var settings = new Settings()
			{
				//BinaryIOFiles = binaryFiles,
				Float64 = doublePrecision 
			};

			using (StreamWriter file = File.CreateText(path))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, settings);
			}

			// For .NET Core 3.0+ and .NET 5+, instead of Newtonsoft lib:
			//string json = JsonSerializer.Serialize(settings);
			//File.WriteAllText(path, json);
		}

		private class Settings
		{
			//public bool BinaryIOFiles { get; set; }
			public bool Float64 { get; set; }
		}
	}
}
