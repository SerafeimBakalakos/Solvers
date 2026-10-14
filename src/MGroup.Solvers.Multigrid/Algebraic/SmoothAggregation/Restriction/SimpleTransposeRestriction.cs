namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Restriction
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class SimpleTransposeRestriction : IRestrictionStrategy
	{
		public CsrMatrix CalcRestrictionMatrix(CsrMatrix prolongationMatrix) => prolongationMatrix.TransposeToCSR();
	}
}
