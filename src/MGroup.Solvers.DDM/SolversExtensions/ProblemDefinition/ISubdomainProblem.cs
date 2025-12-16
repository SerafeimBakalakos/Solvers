namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;

	public interface ISubdomainProblem
	{
		ISubdomainDofOrdering_v2 DofOrdering { get; }

		ISubdomain_v2 Subdomain { get; }

		IMatrix SystemMatrix { get; set; }

		IVector SystemRhs { get; set; }

		IVector SystemSolution { get; set; }

		void OrderDofs();
	}
}
