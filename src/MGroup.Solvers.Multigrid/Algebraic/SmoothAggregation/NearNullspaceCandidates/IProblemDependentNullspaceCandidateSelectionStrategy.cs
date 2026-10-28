namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.LinearSystem;

	public interface IProblemDependentNullspaceCandidateSelectionStrategy : INearNullspaceCandidatesSelectionStrategy
	{
		void SetModel(IModel model, IGlobalAlgebraicModel algebraicModel);
	}
}
