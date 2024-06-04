namespace MGroup.Solvers.MachineLearning.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.MachineLearning.Tests.Dynamic;
	using MGroup.Solvers.MachineLearning.Tests.MLExtensions;

	public class Program
	{
		public static void Main(string[] args)
		{
			CantileverDynamicAnalysis.RunAnalysis();
		}
	}
}
