namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;

	public class SaProlongation
	{
		private readonly INearNullspaceCandidatesSelectionStrategy candidatesSelection;
		private readonly INearNullspaceCandidatesRelaxationStrategy candidatesRelaxation;
		private	readonly IProlongationSmoothingStrategy prolongationSmoothing;
		private readonly CandidateVectorFitting candidatesFitting;

		public SaProlongation(INearNullspaceCandidatesSelectionStrategy candidatesSelection, INearNullspaceCandidatesRelaxationStrategy candidatesRelaxation, CandidateVectorFitting candidatesFitting, IProlongationSmoothingStrategy prolongationSmoothing)
		{
			this.candidatesSelection = candidatesSelection;
			this.candidatesRelaxation = candidatesRelaxation;
			this.candidatesFitting = candidatesFitting;
			this.prolongationSmoothing = prolongationSmoothing;
		}

		public CsrMatrix ComputeProlongationMatrix(CsrMatrix systemMatrix, AggregateCollection aggregates)
		{
			//CsrMatrix AggOp = aggregates.BuildAggregateMembershipMatrix();
			Matrix Bf = candidatesSelection.ChooseCandidateVectors(systemMatrix);
			candidatesRelaxation.ImproveCandidateVectors(systemMatrix, Bf);
			(CsrMatrix Pt, Matrix Bc) = candidatesFitting.FitCandidates(aggregates, Bf);
			CsrMatrix P = prolongationSmoothing.SmoothProlongationMatrix(Pt, systemMatrix);
			return P;
		}
	}
}
