using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

namespace MGroup.Solvers.DDM.SolversExtensions.DofOrdering
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;

	public interface ISubdomainDofOrdering_v2
	{
		IntDofTable Dofs { get; }

		int NumDofs { get; }

		(int[] elementDofIndices, int[] subdomainDofIndices) MapDofsElementToSubdomain(ISuperElement element);

		void OrderDofs();

		void PrepareDofMaps();
	}
}
