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

	public class GlobalSubstructureProblem : ISubstructureProblem
	{
		public GlobalSubstructureProblem(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			Substructure = substructure;
			DofOrdering = dofOrdering;
		}

		public ISubstructureDofOrdering DofOrdering { get; }

		public ISubstructure Substructure { get; }

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
