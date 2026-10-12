namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IStrengthOfConnection
	{
		/// <summary>
		/// Computes the strength-of-connection measure for a real scalar sparse matrix.
		/// </summary>
		/// <param name="A">Square sparse matrix of the linear system.</param>
		/// <returns>Strength matrix C with the same dimensions as A.</returns>
		CsrMatrix CalcStrengthMatrix(CsrMatrix A);
	}
}
