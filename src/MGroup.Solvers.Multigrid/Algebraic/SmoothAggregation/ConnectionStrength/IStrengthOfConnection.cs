namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.ConnectionStrength
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Requirements for the strength matrix C are:
	/// 1) Nonzero diagonal whenever A has a nonzero diagonal
	/// 2) Non-negative entries(float or bool) in [0, 1]
	/// 3) Large entries denoting stronger connections
	/// 4) C denotes nodal connections, i.e., if A is an nxn BSR matrix with row block size of m, then C is (n/m)x(n/m)
	/// </summary>
	public interface IStrengthOfConnection
	{
		/// <summary>
		/// Computes the strength-of-connection measure for a real scalar sparse matrix.
		/// </summary>
		/// <param name="A">Square sparse matrix of the linear system.</param>
		/// <param name="blockSize">
		/// Viewing <paramref name="A"/> as a blocked matrix, what is the number of rows (same as columns) within each block.
		/// </param>
		/// <returns>Strength matrix C with the same dimensions as A.</returns>
		CsrMatrix CalcStrengthMatrix(CsrMatrix A, int blockSize);
	}
}
