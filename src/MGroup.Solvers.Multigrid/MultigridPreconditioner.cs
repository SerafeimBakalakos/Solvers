namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class MultigridPreconditioner : IPreconditioner
	{
		private readonly Action<IReadOnlyVector, IVector> runMGCycle;

		public MultigridPreconditioner(Action<IReadOnlyVector, IVector> runMGCycle)
		{
			this.runMGCycle = runMGCycle;
		}

		public IPreconditioner CopyWithInitialSettings() => new MultigridPreconditioner(runMGCycle);

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			runMGCycle(rhsVector, lhsVector);
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) 
		{
			// Preconditioner is updated by the MG solver directly 
		}
	}
}
