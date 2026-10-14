namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class SystemOrderBasedTermination : IAmgCoarseningTerminationCriterion
	{
		private readonly int targetMinOrder;

		public SystemOrderBasedTermination(int targetMinOrder = 10)
		{
			this.targetMinOrder = targetMinOrder;
		}

		public bool MustStopCoarsening(IReadOnlyMatrix coarsestSystemMatrix) => coarsestSystemMatrix.NumRows <= targetMinOrder;
	}
}
