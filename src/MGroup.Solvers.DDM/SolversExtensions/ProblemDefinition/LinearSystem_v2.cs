namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class LinearSystem_v2
	{
		public IMatrix Matrix { get; set; }

		public IVector RhsVector { get; set; }

		public IVector Solution { get; set; }
	}
}
