namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;

	public static class PreconditionsExtensions
	{
		public static void CheckRowAndColumnPermutation(IIndexable2D matrix, Permutation permutation)
		{
			if ((matrix.NumRows != permutation.Order) || (matrix.NumColumns != permutation.Order))
			{
				string message = $"Matrix has dimensions ({matrix.NumRows}x{matrix.NumColumns})," +
					$" while the permutation assumesns {permutation.Order} rows and columns.";
				throw new NonMatchingDimensionsException(message);
			}
		}
	}
}
