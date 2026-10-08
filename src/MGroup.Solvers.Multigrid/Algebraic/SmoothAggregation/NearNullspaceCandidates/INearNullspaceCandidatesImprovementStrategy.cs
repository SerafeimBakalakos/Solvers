namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface INearNullspaceCandidatesImprovementStrategy
	{
		void ImproveCandidateVectors(Matrix candidatesFine, Matrix candidatesCoarse);
	}
}
