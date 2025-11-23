namespace MGroup.SolverExtensions.DofOrdering
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.SolverExtensions.LinearSystem;
	using MGroup.Solvers;

	public interface ISubstructureDofOrdering
	{
		IntDofTable Dofs { get; }

		int NumDofs { get; }

		(int[] elementDofIndices, int[] substructureDofIndices) MapDofsElementToSubstructure(ISuperElement superElement);

		void Reorder(IReorderingAlgorithm reorderingAlgorithm);
	}
}
