namespace MGroup.Solvers.MachineLearning.Tests.MLExtensions.PythonInterop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop;

	using Xunit;

	public class PythonCallerTests
	{
		[Fact]
		public static void TestVectorLinearCombination()
		{
			string workDirectory = "C:\\Users\\Serafeim\\Desktop\\AISolve\\PythonNetInterop";
			string pythonProjectDirectory = "G:\\Coding\\MGroup\\AISolve\\python_net_interop";
			string pythonInterpreter = pythonProjectDirectory + "\\venv\\Scripts\\python.exe";
			string pythonScript = pythonProjectDirectory + "\\src\\tests\\vector_linear_combination_test.py";
			
			var builder = new PythonCallBuilder(workDirectory, pythonInterpreter, pythonScript);
			builder.DefineSmallInput("CoeffX");
			builder.DefineSmallInput("CoeffY");
			builder.DefineArrayInput("VectorX");
			builder.DefineArrayInput("VectorY");
			builder.DefineArrayOutput("VectorZ");
			PythonCall pythonCall = builder.Build();

			pythonCall.PassSmallInput("CoeffX", "2.0");
			pythonCall.PassArrayInput("VectorX", new double[] { 1000.0, 1100.0, 1200.0, 1300 });
			pythonCall.PassSmallInput("CoeffY", "3.0");
			pythonCall.PassArrayInput("VectorY", new double[] { 1.0, 2.0, 3.0, 4.0 });
			pythonCall.Execute();

			var expected = Vector.CreateFromArray(new double[] { 2003.0, 2206.0, 2409.0, 2612.0 });
			var computed = Vector.CreateFromArray(pythonCall.GetArrayOutput("VectorZ"));
			Assert.True(expected.Equals(computed));
		}
	}
}
