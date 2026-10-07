namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class NoNearNullspaceCandidatesImprovement : INearNullspaceCandidatesImprovementStrategy
	{
		public void ImproveCandidateVectors(Matrix candidatesFine, Matrix candidatesCoarse) => throw new NotImplementedException();
	}
}
