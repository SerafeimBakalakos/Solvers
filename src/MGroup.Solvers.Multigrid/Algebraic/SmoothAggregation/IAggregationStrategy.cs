namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Calculates the aggregates given a sparse matrix.
	/// </summary>
	public interface IAggregationStrategy
	{
		/// <summary>
		/// Creates the aggregates.
		/// </summary>
		/// <param name="soc">
		/// The strength of connection matrix, interpreted as a graph through its CSR structure. Numerical values of the matrix are not used.
		/// </param>
		/// <returns>
		/// The returned aggregate indices are zero-based. An isolated node that cannot be aggregated is assigned -1.
		/// </returns>
		AggregateCollection FindAggregates(CsrMatrix soc);
	}
}
