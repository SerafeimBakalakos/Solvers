namespace MGroup.Solvers.DDM.SolversExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface ISubstructureSystemSolver
	{
		bool CanOverwriteSystemMatrices { get; set; }

		ISubstructureDofOrdering DofOrdering { get; set; }

		IMatrix Matrix { get; set; }

		IVector Rhs { get; set; }

		IVector Solution { get; set; }

		ISubstructure Substructure { get; set; }

		void PrepareDofs();

		void PrepareLinearSystem();

		void SolveLinearSystem();
	}
}
