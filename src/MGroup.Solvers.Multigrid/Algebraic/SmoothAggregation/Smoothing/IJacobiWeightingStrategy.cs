namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IJacobiWeightingStrategy
	{
		CsrMatrix WeightSystemMatrix(CsrMatrix systemMatrix);
	}
}
