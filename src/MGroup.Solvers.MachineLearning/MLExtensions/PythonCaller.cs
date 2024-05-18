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

		public PythonCaller(string workDir, string pythonInterpreter, string pythonScript, bool cleanupIOFiles = true, 
			int timeoutMilliseconds = -1)
		{
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.pythonScript = pythonScript;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;
		}

		public float CallPython(float input)
		{
			// Setup IO files
			var time = DateTime.Now;
			string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}{time.Second}";
			//string filename = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";

			string inputFile = $"{workDir}\\{filename}_in.txt";
			string outputFile = $"{workDir}\\{filename}_out.txt";
			using (var f = File.Open(inputFile, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					writer.Write(input);
				}
			}

			// Run process
			var startInfo = new ProcessStartInfo(pythonInterpreter);
			startInfo.FileName = pythonInterpreter;
			startInfo.Arguments = $"{pythonScript} {inputFile} {outputFile}";
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardOutput = true;
			using (var process = Process.Start(startInfo))
			{
				process.WaitForExit(timeoutMilliseconds);
			}

			// Read result from output file
			float result = float.NaN;
			using (var reader = new StreamReader(outputFile))
			{
				result = float.Parse(reader.ReadLine());
			}

			// Cleanup
			if (cleanupIOFiles)
			{
				File.Delete(inputFile);
				File.Delete(outputFile);
			}

			return result;
		}
	}
}
