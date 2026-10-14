namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class NoNearNullspaceCandidatesRelaxation : INearNullspaceCandidatesRelaxationStrategy
	{
		public INearNullspaceCandidatesRelaxationStrategy CopyWithInitialSettings() => new NoNearNullspaceCandidatesRelaxation();

		public void ImproveCandidateVectors(IReadOnlyMatrix systemMatrix, Matrix candidateVectors) { }
	}
}
