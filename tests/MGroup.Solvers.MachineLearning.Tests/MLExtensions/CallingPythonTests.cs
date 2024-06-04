#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
namespace MGroup.Solvers.MachineLearning.Tests.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.MachineLearning.MLExtensions;

	using Xunit;

	public static class CallingPythonTests
	{
		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public static void TestEvaluate(bool doublePrecision, bool binaryIOFiles)
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge\\eval";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string pythonScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\src\\surrogates\\evaluate.py";

			var pythonCaller = new PythonCaller(workDir, pythonInterpreter, pythonScript);
			pythonCaller.UseBinaryIOFilesForArrays = binaryIOFiles;
			pythonCaller.CleanupIOFiles = true;

			double[] x = { 1.11, 3.33, -2.22 };
			double[] yExpected = { 2.22, 6.66, -4.44 };
			double tol = 1E-10;
			if (doublePrecision)
			{
				double[] yComputed = pythonCaller.CallPython(x);
				CheckArray(yExpected, yComputed, tol);
			}
			else
			{
				float[] yComputed = pythonCaller.CallPython(x.ToFloat32());
				CheckArray(yExpected.ToFloat32(), yComputed, (float)tol);
			}
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public static void TestTrainAndPredict(bool doublePrecision, bool binaryIOFiles)
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge\\sample_surrogate";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string trainScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\src\\surrogates\\train.py";
			string predictScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\src\\surrogates\\predict.py";

			var surrogate = new PythonSurrogate(workDir, pythonInterpreter, trainScript, predictScript, sizeInput:2, sizeOutput:3);
			surrogate.UseBinaryIOFilesForArrays = binaryIOFiles;
			surrogate.TensorFlowSeed = 40;
			surrogate.CleanupIOFiles = true;

			double[,] features =
			{
				{ 0.25, 1.36 },
				{ 0.42, 1.53 },
				{ 0.61, 1.72 },
				{ 0.85, 1.96 },
				{ 1.02, 2.13 },
				{ 1.25, 2.36 },
				{ 1.40, 2.51 },
				{ 1.67, 2.78 },
				{ 1.90, 3.01 },
				{ 2.21, 3.32 },
			};
			double[,] labels =
			{
				{ 1.20, 3.40, -0.10 },
				{ 1.50, 4.00, -0.25 },
				{ 1.80, 4.60, -0.40 },
				{ 1.95, 4.90, -0.48 },
				{ 2.27, 5.54, -0.64 },
				{ 2.59, 6.18, -0.80 },
				{ 2.80, 6.60, -0.90 },
				{ 3.12, 7.24, -1.06 },
				{ 3.46, 7.92, -1.23 },
				{ 3.83, 8.66, -1.42 },
			};
			double[] x = { 1.02, 2.13 };
			double[] yExpected = { 2.27, 5.54, -0.64 };
			double tolerance = 0.1;

			if (doublePrecision)
			{
				surrogate.CallTrainScript(features, labels);
				double[] yComputed = surrogate.CallPredictScript(x);
				CheckArray(yExpected, yComputed, tolerance);
			}
			else
			{
				surrogate.CallTrainScript(features.ToFloat32(), labels.ToFloat32());
				float[] yComputed = surrogate.CallPredictScript(x.ToFloat32());
				CheckArray(yExpected.ToFloat32(), yComputed, (float)tolerance);
			}
		}

		private static void CheckArray(double[] expected, double[] actual, double tolerance)
		{
			Assert.True(expected.Length == actual.Length);
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.Equal(expected[i], actual[i], tolerance);
			}
		}

		private static void CheckArray(float[] expected, float[] actual, float tolerance)
		{
			Assert.True(expected.Length == actual.Length);
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.Equal(expected[i], actual[i], tolerance);
			}
		}
	}
}
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
