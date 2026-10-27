namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.Algorithms;

	public class MultigridPreconditioner : IPreconditioner
	{
		private readonly IMultigridAlgorithm multigrid;

		public MultigridPreconditioner(IMultigridAlgorithm multigrid)
		{
			this.multigrid = multigrid;
		}

		public IPreconditioner CopyWithInitialSettings() => new MultigridPreconditioner(multigrid);

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			lhsVector.Clear();
			multigrid.RunSingleCycle((Vector)rhsVector, (Vector)lhsVector);
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) 
		{
			// Preconditioner is updated by the MG solver directly 
		}
	}
}
