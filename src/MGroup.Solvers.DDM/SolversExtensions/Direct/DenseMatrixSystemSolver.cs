namespace MGroup.Solvers.DDM.SolversExtensions.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DenseMatrixSystemSolver
	{
		private readonly ISubstructureProblemSystem linearSystem;

		public DenseMatrixSystemSolver(ISubstructureProblemSystem linearSystem)
		{
			this.linearSystem = linearSystem;
		}

		public void BuildMatrix()
		{

		}

		public void Solve()
		{

		}
	}
}
