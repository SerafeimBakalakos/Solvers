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
		public static void RunTest()
		{
			string workDir = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonCSharpBridge";
			string pythonInterpreter = "C:\\Coding\\Dev\\Python\\DDM_ML\\venv\\Scripts\\python.exe";
			//string pythonScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\main.py";
			string pythonScript = "C:\\Coding\\Dev\\Python\\DDM_ML\\surrogates\\evaluate.py";

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
	}
}
