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
		private readonly INearNullspaceCandidatesImprovementStrategy candidatesImprovement;
		private	readonly IProlongationSmoothingStrategy prolongationSmoothing;
		private readonly TentativeProlongation tentativeProlongation = new();

		public SaProlongation(INearNullspaceCandidatesSelectionStrategy candidatesSelection, INearNullspaceCandidatesImprovementStrategy candidatesImprovement, IProlongationSmoothingStrategy prolongationSmoothing)
		{
			this.candidatesSelection = candidatesSelection;
			this.candidatesImprovement = candidatesImprovement;
			this.prolongationSmoothing = prolongationSmoothing;
		}

		public CsrMatrix ComputeProlongationMatrix(CsrMatrix systemMatrix, AggregateCollection aggregates)
		{
			CsrMatrix A = aggregates.BuildAggregateMembershipMatrix();
			CsrMatrix Pt = ComputeTentativeProlongation(A);
			CsrMatrix P = prolongationSmoothing.SmoothProlongationMatrix(Pt, systemMatrix);
			return P;
		}

		private CsrMatrix ComputeTentativeProlongation(CsrMatrix aggregateMembershipMatrix)
		{
			if ((candidatesSelection is ConstantCandidateSelection) && (candidatesImprovement is NoNearNullspaceCandidatesImprovement))
			{
				return aggregateMembershipMatrix;
			}
			else
			{
				(Matrix Bf, Matrix Bc) = candidatesSelection.ChooseCandidateVectors();
				candidatesImprovement.ImproveCandidateVectors(Bf, Bc);
				CsrMatrix Pt = tentativeProlongation.Compute(aggregateMembershipMatrix, Bf, Bc);
				return Pt;
			}
		}
	}
}
