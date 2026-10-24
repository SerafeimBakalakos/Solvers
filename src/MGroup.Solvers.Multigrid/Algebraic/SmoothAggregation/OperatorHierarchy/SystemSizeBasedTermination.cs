namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class SystemSizeBasedTermination : IAmgCoarseningTerminationCriterion
	{
		private readonly int targetMinSize;

		public SystemSizeBasedTermination(int targetMinSize = 10)
		{
			this.targetMinSize = targetMinSize;
		}

		public bool MustStopCoarsening(IReadOnlyMatrix coarsestSystemMatrix) => coarsestSystemMatrix.NumRows <= targetMinSize;
	}
}
