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
		private readonly bool binaryFiles;
		private readonly bool cleanupIOFiles;
		private readonly int timeoutMilliseconds;

		private readonly IArrayFileIO arrayIO;

		public PythonCaller(string workDir, string pythonInterpreter, string pythonScript, bool binaryFiles=false, 
			bool cleanupIOFiles = true, int timeoutMilliseconds = -1)
		{
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.pythonScript = pythonScript;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;
			this.binaryFiles = binaryFiles;
			if (binaryFiles)
			{
				this.arrayIO = new ArrayBinaryFIleIO();
			}
			else
			{
				this.arrayIO = new ArrayTextFileIO(' ');
			}	
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
			//startInfo.RedirectStandardError = true;
			int exitCode = -1;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(timeoutMilliseconds);
				exitCode = process.ExitCode;
			}
			if (exitCode != 0)
			{
				throw new Exception($"Python script exited with code {exitCode}, instead of 0 (successful)");
			}
		}

		private (string inputFile, string outputFile) CreateFilenames()
		{
			// Setup IO files
			var time = DateTime.Now;
			string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}{time.Second}";
			//string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";

			string extension = binaryFiles ? "npy" : "txt";
			string inputFile = $"{workDir}\\{filename}_in.{extension}";
			string outputFile = $"{workDir}\\{filename}_out.{extension}";

			return (inputFile, outputFile);
		}
	}
}
