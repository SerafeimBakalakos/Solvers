namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IRestrictionStrategy
	{
		CsrMatrix CalcRestrictionMatrix(CsrMatrix prolongationMatrix);
	}
}
