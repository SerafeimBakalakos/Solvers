namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearSystem;

	public interface ISolver_v2
	{
		bool CanOverwriteSystemMatrices { get; set; }

		LinearSystem_v2 LinearSystem { get; }

		IDomain Domain { get; }

		IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel);

		void PrepareDofs();

		void BuildSystemMatrix();

		void SolveLinearSystem();
	}
}
