namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;

	public class PythonCaller
	{
		private readonly string workDir;
		private readonly string pythonInterpreter;
		private readonly string pythonScript;
		private readonly bool cleanupIOFiles;
		private readonly int timeoutMilliseconds;

		private readonly ArrayTextFileIO arrayIO;

		public PythonCaller(string workDir, string pythonInterpreter, string pythonScript, bool cleanupIOFiles = true, 
			int timeoutMilliseconds = -1)
		{
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.pythonScript = pythonScript;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;

			this.arrayIO = new ArrayTextFileIO(' ');
		}

		public double[] CallPython(double[] input)
		{
			(string inputFile, string outputFile) = CreateFilenames();

			try
			{
				arrayIO.WriteArray1DToFile(input, inputFile);
				CallPythonScript(inputFile, outputFile);
				var output = new double[input.Length];
				arrayIO.ReadArray1DFromFile(output, outputFile);
				return output;
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(inputFile);
					File.Delete(outputFile);
				}
			}
		}

		public float[] CallPython(float[] input)
		{
			(string inputFile, string outputFile) = CreateFilenames();

			try
			{
				arrayIO.WriteArray1DToFile(input, inputFile);
				CallPythonScript(inputFile, outputFile);
				var output = new float[input.Length];
				arrayIO.ReadArray1DFromFile(output, outputFile);
				return output;
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(inputFile);
					File.Delete(outputFile);
				}
			}
		}

		private void CallPythonScript(string inputFile, string outputFile)
		{
			var startInfo = new ProcessStartInfo(pythonInterpreter);
			startInfo.FileName = pythonInterpreter;
			startInfo.Arguments = $"{pythonScript} {inputFile} {outputFile}";
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(timeoutMilliseconds);
			}
		}

		private (string inputFile, string outputFile) CreateFilenames()
		{
			// Setup IO files
			var time = DateTime.Now;
			string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}{time.Second}";
			//string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";

			string inputFile = $"{workDir}\\{filename}_in.txt";
			string outputFile = $"{workDir}\\{filename}_out.txt";

			return (inputFile, outputFile);
		}
	}
}
