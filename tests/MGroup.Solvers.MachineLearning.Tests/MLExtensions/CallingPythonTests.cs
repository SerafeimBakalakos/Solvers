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
	}
}
