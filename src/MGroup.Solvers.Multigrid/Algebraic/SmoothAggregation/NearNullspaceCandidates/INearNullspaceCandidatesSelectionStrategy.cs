namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface INearNullspaceCandidatesSelectionStrategy
	{
		(Matrix candidatesFine, Matrix candidatesCoarse) ChooseCandidateVectors();
	}
}
