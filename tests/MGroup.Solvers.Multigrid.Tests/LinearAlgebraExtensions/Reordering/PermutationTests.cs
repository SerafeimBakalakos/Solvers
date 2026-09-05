namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Reordering
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;

	using Xunit;

	public static class PermutationTests
	{
		private static int[] ForwardPermutation => new int[] { 0, 1, 5, 6, 7, 8, 9, 4, 2, 3 };
		private static int[] InversePermutation => new int[] { 0, 1, 8, 9, 7, 2, 3, 4, 5, 6 };

		[Fact]
		public static void TestForwardPermutation()
		{
			var permutation = Permutation.CreateWithForwardPermutation(ForwardPermutation);
			int[] p = permutation.PermutationArrayLazy;
			int[] ip = permutation.InversePermutationArrayLazy;
			Assert.Equal(ForwardPermutation, p);
			Assert.Equal(InversePermutation, ip);

		}

		[Fact]
		public static void TestInversePermutation()
		{
			var permutation = Permutation.CreateWithInversePermutation(InversePermutation);
			int[] p = permutation.PermutationArrayLazy;
			int[] ip = permutation.InversePermutationArrayLazy;
			Assert.Equal(ForwardPermutation, p);
			Assert.Equal(InversePermutation, ip);
		}
	}
}
