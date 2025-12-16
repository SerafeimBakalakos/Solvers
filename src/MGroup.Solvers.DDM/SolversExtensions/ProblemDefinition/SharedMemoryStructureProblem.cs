namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;

	public class SharedMemoryStructureProblem : ISubdomainProblem
	{
		public SharedMemoryStructureProblem(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering)
		{
			Subdomain = subdomain;
			DofOrdering = dofOrdering;
		}

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public ISubdomain_v2 Subdomain { get; }

		public IMatrix SystemMatrix { get; set; }

		public IVector SystemRhs { get; set; }

		public IVector SystemSolution { get; set; }

		public void OrderDofs()
		{
			DofOrdering.OrderDofs();
			DofOrdering.PrepareDofMaps();

			SystemMatrix = null;
			SystemRhs = Vector.CreateZero(DofOrdering.NumDofs);
			SystemSolution = Vector.CreateZero(DofOrdering.NumDofs);
		}
	}
}
