namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class ConstantCandidateSelection : INearNullspaceCandidatesSelectionStrategy
	{
		public Matrix ChooseCandidateVectors(IReadOnlyMatrix systemMatrix)
		{
			int n = systemMatrix.NumRows;
			return Matrix.CreateWithValue(n, 1, 1.0);
		}
	}
}
