namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;

	public class PythonSurrogate
	{
		private readonly string workDir;
		private readonly string pythonInterpreter;
		private readonly string trainScript;
		private readonly string predictScript;
		private readonly int sizeInput;
		private readonly int sizeOutput;
		private readonly bool cleanupIOFiles;
		private readonly int timeoutMilliseconds;

		private readonly ArrayTextFileIO arrayIO;

		public PythonSurrogate(string workDir, string pythonInterpreter, string trainScript, string predictScript,
			int sizeInput, int sizeOutput, bool cleanupIOFiles = true, int timeoutMilliseconds = -1)
		{
			this.workDir = workDir;
			this.pythonInterpreter = pythonInterpreter;
			this.trainScript = trainScript;
			this.predictScript = predictScript;
			this.sizeInput = sizeInput;
			this.sizeOutput = sizeOutput;
			this.cleanupIOFiles = cleanupIOFiles;
			this.timeoutMilliseconds = timeoutMilliseconds;

			this.arrayIO = new ArrayTextFileIO(' ');
		}

		public double[] CallPredictScript(double[] input)
		{
			CheckPredictionData(input);
			string tempFilePrefix = GetTempFilePathPrefix();
			string pathInput = tempFilePrefix + "_input.txt";
			string pathOutput = tempFilePrefix + "_output.txt";
			string pathModel = GetModelPath();
			try
			{
				arrayIO.WriteArray1DToFile(input, pathInput);
				CallPythonScript(predictScript, pathInput, pathOutput, pathModel);
				var output = new double[sizeOutput];
				arrayIO.ReadArray1DFromFile(output, pathOutput);
				return output;
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(pathInput);
					File.Delete(pathOutput);
				}
			}
		}

		public float[] CallPredictScript(float[] input)
		{
			CheckPredictionData(input);
			string tempFilePrefix = GetTempFilePathPrefix();
			string pathInput = tempFilePrefix + "_input.txt";
			string pathOutput = tempFilePrefix + "_output.txt";
			string pathModel = GetModelPath();
			try
			{
				arrayIO.WriteArray1DToFile(input, pathInput);
				CallPythonScript(predictScript, pathInput, pathOutput, pathModel);
				var output = new float[sizeOutput];
				arrayIO.ReadArray1DFromFile(output, pathOutput);
				return output;
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(pathInput);
					File.Delete(pathOutput);
				}
			}
		}

		public void CallTrainScript(double[,] features, double[,] labels)
		{
			CheckTrainData(features, labels);
			string tempFilePrefix = GetTempFilePathPrefix();
			string pathFeatures = tempFilePrefix + "_features.txt";
			string pathLabels = tempFilePrefix + "_labels.txt";
			string pathModel = GetModelPath();
			try
			{
				arrayIO.WriteArray2DToFile(features, pathFeatures);
				arrayIO.WriteArray2DToFile(labels, pathLabels);
				CallPythonScript(trainScript, pathFeatures, pathLabels, pathModel);
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(pathFeatures);
					File.Delete(pathLabels);
				}
			}
		}

		public void CallTrainScript(float[,] features, float[,] labels)
		{
			CheckTrainData(features, labels);
			string tempFilePrefix = GetTempFilePathPrefix();
			string pathFeatures = tempFilePrefix + "_features.txt";
			string pathLabels = tempFilePrefix + "_labels.txt";
			string pathModel = GetModelPath();
			try
			{
				arrayIO.WriteArray2DToFile(features, pathFeatures);
				arrayIO.WriteArray2DToFile(labels, pathLabels);
				CallPythonScript(trainScript, pathFeatures, pathLabels, pathModel);
			}
			finally
			{
				// Cleanup
				if (cleanupIOFiles)
				{
					File.Delete(pathFeatures);
					File.Delete(pathLabels);
				}
			}
		}

		private void CallPythonScript(string scriptFile, string pathFeatures, string pathLabels, string pathModel)
		{
			var startInfo = new ProcessStartInfo(pythonInterpreter);
			startInfo.FileName = pythonInterpreter;
			startInfo.Arguments = $"{scriptFile} {pathFeatures} {pathLabels} {pathModel}";
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
			Console.WriteLine();
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

		private string GetModelPath()
		{
			return $"{workDir}\\model.keras";
		}

		private string GetTempFilePathPrefix()
		{
			var time = DateTime.Now;
			string prefix = $"{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";
			return $"{workDir}\\{prefix}";
		}
	}
}
