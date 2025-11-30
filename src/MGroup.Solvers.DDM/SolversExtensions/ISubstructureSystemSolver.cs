namespace MGroup.Solvers.DDM.SolversExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface ISubstructureSystemSolver
	{
		bool CanOverwriteSystemMatrices { get; set; }

		ISubstructureProblem Problem { get; }

		ISubstructure Substructure { get; }

		void PrepareDofs();

		void BuildSystemMatrix();

		void SolveLinearSystem();
	}
}
