namespace MGroup.Solvers.DDM.SolversExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface ISubdomainSystemSolver
	{
		bool CanOverwriteSystemMatrices { get; set; }

		LinearSystem_v2 LinearSystem { get; }

		ISubdomain_v2 Subdomain { get; }

		IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel);

		void PrepareDofs();

		void BuildSystemMatrix();

		void SolveLinearSystem();
	}
}
