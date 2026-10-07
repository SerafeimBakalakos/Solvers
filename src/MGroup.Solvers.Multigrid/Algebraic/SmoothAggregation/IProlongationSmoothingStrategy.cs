namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IProlongationSmoothingStrategy
	{
		CsrMatrix SmoothProlongationMatrix(CsrMatrix prolongationMatrix, CsrMatrix systemMatrix);
	}
}
