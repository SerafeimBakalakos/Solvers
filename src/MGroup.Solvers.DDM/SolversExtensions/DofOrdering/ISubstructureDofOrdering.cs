using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

namespace MGroup.Solvers.DDM.SolversExtensions.DofOrdering
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;

	public interface ISubstructureDofOrdering
	{
		IntDofTable Dofs { get; }

		int NumDofs { get; }

		(int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement);

		void OrderDofs();

		void PrepareDofMaps();
	}
}
