namespace MGroup.Solvers.MachineLearning.Tests.MLExtensions.PythonInterop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop;

	using Xunit;

	public class PythonCallerTests
	{
		[Fact]
		public static void TestVectorLinearCombination()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonNetInterop";
			string pythonProjectDirectory = "C:\\Coding\\Dev\\Python\\cs2py_ml_surrogates";
			string pythonInterpreter = pythonProjectDirectory + "\\venv\\Scripts\\python.exe";
			string pythonScript = pythonProjectDirectory + "\\src\\cae_ffnn_dynamic_t_as_param\\train.py";

			var builder = new PythonCallBuilder(workDirectory, pythonInterpreter, pythonScript);
			builder.DefineSmallInput("CoeffX");
			builder.DefineSmallInput("CoeffY");
			builder.DefineArrayInput("VectorX");
			builder.DefineArrayInput("VectorY");
			PythonCall pythonCall = builder.Build();

			pythonCall.PassSmallInput("CoeffX", "2.0");
			pythonCall.PassArrayInput("VectorX", new double[] { 1000.0, 1100.0, 1200.0, 1300 });
			pythonCall.PassSmallInput("CoeffY", "3.0");
			pythonCall.PassArrayInput("VectorY", new double[] { 1.0, 2.0, 3.0, 4.0 });
			pythonCall.Execute();
		}
	}
}
