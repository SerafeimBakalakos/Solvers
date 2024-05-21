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
		[Fact]
		public static void TestDouble()
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string pythonScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\evaluate_double.py";

			var pythonCaller = new PythonCaller(workDir, pythonInterpreter, pythonScript, false);

			double[] x = { 1.11, 3.33, -2.22 };
			double[] yExpected = { 2.22, 6.66, -4.44 };
			double[] yComputed = pythonCaller.CallPython(x);

			double tol = 1E-10;
			for (int i = 0; i < x.Length; i++)
			{
				Assert.Equal(yExpected[i], yComputed[i], tol);
			}
		}

		[Fact]
		public static void TestSingle()
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string pythonScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\evaluate_single.py";

			var pythonCaller = new PythonCaller(workDir, pythonInterpreter, pythonScript, false);

			float[] x = { 1.11f, 3.33f, -2.22f };
			float[] yExpected = { 2.22f, 6.66f, -4.44f };
			float[] yComputed = pythonCaller.CallPython(x);

			double tol = 1E-8;
			for (int i = 0; i < x.Length; i++)
			{
				Assert.Equal(yExpected[i], yComputed[i], tol);
			}
		}

		[Fact]
		public static void TestTrainAndPredict()
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string trainScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\train.py";
			string predictScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\predict.py";

			var surrogate = new PythonSurrogate(workDir, pythonInterpreter, trainScript, predictScript, 2, 3, true);

			float[,] features =
			{
				{ 0.25f, 1.36f },
				{ 0.42f, 1.53f },
				{ 0.61f, 1.72f },
				{ 0.85f, 1.96f },
				{ 1.02f, 2.13f },
				{ 1.25f, 2.36f },
				{ 1.40f, 2.51f },
				{ 1.67f, 2.78f },
				{ 1.90f, 3.01f },
				{ 2.21f, 3.32f },
			};
			float[,] labels =
			{
				{ 1.20f, 3.40f, -0.10f },
				{ 1.50f, 4.00f, -0.25f },
				{ 1.80f, 4.60f, -0.40f },
				{ 1.95f, 4.90f, -0.48f },
				{ 2.27f, 5.54f, -0.64f },
				{ 2.59f, 6.18f, -0.80f },
				{ 2.80f, 6.60f, -0.90f },
				{ 3.12f, 7.24f, -1.06f },
				{ 3.46f, 7.92f, -1.23f },
				{ 3.83f, 8.66f, -1.42f },
			};
			surrogate.CallTrainScript(features, labels);

			float[] x = { 1.02f, 2.13f };
			float[] yExpected = { 2.27f, 5.54f, -0.64f };
			float[] yComputed = surrogate.CallPredictScript(x);

			float tol = 0.1f;
			for (int i = 0; i < x.Length; i++)
			{
				Assert.Equal(yExpected[i], yComputed[i], tol);
			}
		}

		[Fact]
		public static void TestTrainAndPredictDouble()
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			string trainScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\train_double.py";
			string predictScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\predict_double.py";

			var surrogate = new PythonSurrogate(workDir, pythonInterpreter, trainScript, predictScript, 2, 3, true);

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
				{ 1.50, 4.00, -0.25	},
				{ 1.80, 4.60, -0.40	},
				{ 1.95, 4.90, -0.48	},
				{ 2.27, 5.54, -0.64	},
				{ 2.59, 6.18, -0.80	},
				{ 2.80, 6.60, -0.90	},
				{ 3.12, 7.24, -1.06	},
				{ 3.46, 7.92, -1.23	},
				{ 3.83, 8.66, -1.42 },
			};
			surrogate.CallTrainScript(features, labels);

			double[] x = { 1.02, 2.13 };
			double[] yExpected = { 2.27, 5.54, -0.64 };
			double[] yComputed = surrogate.CallPredictScript(x);

			double tol = 0.1;
			for (int i = 0; i < x.Length; i++)
			{
				Assert.Equal(yExpected[i], yComputed[i], tol);
			}
		}
	}
}
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
