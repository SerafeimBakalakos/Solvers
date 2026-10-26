namespace MGroup.Solvers.Multigrid.Restriction
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class UnscaledTransposeRestriction : IRestrictionStrategy
	{
		public CsrMatrix CalcRestrictionMatrix(CsrMatrix prolongationMatrix) => prolongationMatrix.TransposeToCSR();
	}
}
