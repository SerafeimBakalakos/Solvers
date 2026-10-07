namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class ConstantCandidateSelection : INearNullspaceCandidatesSelectionStrategy
	{
		public (Matrix candidatesFine, Matrix candidatesCoarse) ChooseCandidateVectors() => throw new NotImplementedException();
	}
}
