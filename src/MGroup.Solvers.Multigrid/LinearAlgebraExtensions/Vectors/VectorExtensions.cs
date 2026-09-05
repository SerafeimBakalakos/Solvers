namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Vectors
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;

	public static class VectorExtensions
	{
		public static Vector Permute(this Vector original, Permutation permutation)
		{
			Preconditions.CheckVectorDimensions(original.Length, permutation.Order);
			var result = Vector.CreateZero(original.Length);
			int[] inversePermutation = permutation.InversePermutationArrayLazy;
			Permutations.PermuteVector(original.RawData, inversePermutation, result.RawData);
			return result;
		}

		public static void PermuteIntoResult(this Vector original, Permutation permutation, Vector result)
		{
			Preconditions.CheckVectorDimensions(original, result);
			Preconditions.CheckVectorDimensions(original.Length, permutation.Order);
			int[] inversePermutation = permutation.InversePermutationArrayLazy;
			Permutations.PermuteVector(original.RawData, inversePermutation, result.RawData);
		}
	}
}
