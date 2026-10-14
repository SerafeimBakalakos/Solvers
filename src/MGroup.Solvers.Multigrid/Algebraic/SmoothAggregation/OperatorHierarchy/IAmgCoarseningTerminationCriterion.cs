namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IAmgCoarseningTerminationCriterion
	{
		bool MustStopCoarsening(IReadOnlyMatrix coarsestSystemMatrix);
	}
}
