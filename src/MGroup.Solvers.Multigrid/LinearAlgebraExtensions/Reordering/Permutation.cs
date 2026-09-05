namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	public class Permutation
	{
		private readonly object syncLock = new();

		private int[]? permutationArray;
		private int[]? inversePermutationArray;

		public Permutation(int order)
		{
			this.Order = order;
		}

		public int Order { get; }

		public int[] InversePermutationArrayLazy
		{
			get
			{
				lock (syncLock)
				{
					if (inversePermutationArray is null)
					{
						Debug.Assert(permutationArray is not null);
						inversePermutationArray = InvertArray(permutationArray);
					}
				}

				return inversePermutationArray;
			}
		}

		public int[] PermutationArrayLazy
		{
			get
			{
				lock (syncLock)
				{
					if (permutationArray is null)
					{
						Debug.Assert(inversePermutationArray is not null);
						permutationArray = InvertArray(inversePermutationArray);
					}
				}
				
				return permutationArray;
			}
		}

		public static Permutation CreateWithForwardPermutation(int[] permutationArray)
		{
			var permutation = new Permutation(permutationArray.Length);
			permutation.permutationArray = permutationArray;
			return permutation;
		}

		public static Permutation CreateWithInversePermutation(int[] inversePermutationArray)
		{
			var permutation = new Permutation(inversePermutationArray.Length);
			permutation.inversePermutationArray = inversePermutationArray;
			return permutation;
		}

		public Permutation Invert()
		{
			if (permutationArray is null)
			{
				Debug.Assert(inversePermutationArray is not null);
				permutationArray = InvertArray(inversePermutationArray);
			}
			else
			{
				inversePermutationArray = InvertArray(permutationArray);
			}

			var inverse = new Permutation(Order);
			inverse.permutationArray = inversePermutationArray;
			inverse.inversePermutationArray = permutationArray;
			return inverse;
		}

		private static int[] InvertArray(int[] permutation)
		{
			int length = permutation.Length;
			var result = new int[length];
			for (int i = 0; i < length; i++)
			{
				result[permutation[i]] = i;
			}

			return result;
		}
	}
}
