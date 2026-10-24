namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;

	public interface INullspaceCandidatesFittingStrategy
	{
		(CsrMatrix tentativeProlongation, Matrix coarseCandidates, int blockSizeCoarse) FitCandidates(AggregateCollection aggregates, Matrix candidatesFine);
	}
}
