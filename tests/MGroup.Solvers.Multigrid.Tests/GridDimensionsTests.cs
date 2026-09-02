namespace MGroup.Solvers.Multigrid.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public static class GridDimensionsTests
	{
		[Fact]
		public static void TestGridDimensions1D_UniformRatio2()
		{ 
			// 17 nodes -> 9 -> 5 -> 3
			// With 4 levels, ratio 2 remains feasible at every step.
			var gridDimensions = GridDimensions.Create1D(numLevels: 4, numNodesFine: 17, coarsenRatio: 2);
			int[,] expected = { { 17 }, { 9 }, { 5 }, { 3 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions1D_RatioIsReducedWhenFullCoarseningIsNoLongerFeasible()
		{
			// 13 nodes = 12 elements: ratio 3 -> 5 nodes
			// 5 nodes = 4 elements: ratio 3 is no longer feasible
			// reduce ratio to 2 -> 3 nodes
			// 3 nodes = 2 elements: ratio 2 -> 2 nodes
			// The ratio remains 2 for the remaining levels.
			var gridDimensions = GridDimensions.Create1D(numLevels: 4, numNodesFine: 13, coarsenRatio: 3);
			int[,] expected = { { 13 }, { 5 }, { 3 }, { 2 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions2D_UniformRatio2()
		{ 
			// Both axes coarsen by 2 at every level.
			var gridDimensions = GridDimensions.Create2D(numLevels: 4, numNodesFine: new[] { 9, 17 }, coarsenRatios: new[] { 2, 2 });
			int[,] expected = { { 9, 17 }, { 5, 9 }, { 3, 5 }, { 2, 3 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions2D_DifferentRatios()
		{ 
			// X coarsens by 2, Y coarsens by 3.
			var gridDimensions = GridDimensions.Create2D(numLevels: 3, numNodesFine: new[] { 17, 28 }, coarsenRatios: new[] { 2, 3 });
			int[,] expected = { { 17, 28 }, { 9, 10 }, { 5, 4 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions2D_RatioOnOneAxisIsReducedToOne()
		{ 
			// X can continue coarsening by 2. Y eventually cannot, so its ratio is reduced and ultimately becomes 1. 
			// X: 17 -> 9 -> 5 -> 3 -> 2 
			// Y: 5 -> 3 -> 2 -> 2 -> 2
			var gridDimensions = GridDimensions.Create2D(numLevels: 5, numNodesFine: new[] { 17, 5 }, coarsenRatios: new[] { 2, 2 });
			int[,] expected = { { 17, 5 }, { 9, 3 }, { 5, 2 }, { 3, 2 }, { 2, 2 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact] 
		public static void TestGridDimensions2D_ReducesXRatioWhileYContinuesCoarsening() 
		{
			// X starts with ratio 4:
			// 25 nodes -> 7 nodes
			// 7 nodes: ratio 4 is not feasible, reduce to 3 -> 3 nodes
			// 3 nodes: ratio 3 is not feasible, reduce to 2 -> 2 nodes
			// 2 nodes: ratio 2 is not feasible, reduce to 1 -> 2 nodes
			
			// Y continues using ratio 2:
			// 65 -> 33 -> 17 -> 9 -> 5
			
			var gridDimensions = GridDimensions.Create2D(numLevels: 5, numNodesFine: new[] { 25, 65 }, coarsenRatios: new[] { 4, 2 });
			int[,] expected = { { 25, 65 }, { 7, 33 }, { 3, 17 }, { 2, 9 }, { 2, 5 } }; 
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions3D_UniformRatio2()
		{ 
			// All three axes coarsen by 2.
			var gridDimensions = GridDimensions.Create3D(numLevels: 4, numNodesFine: new[] { 33, 17, 25 }, coarsenRatios: new[] { 2, 2, 2 });
			int[,] expected = { { 33, 17, 25 }, { 17, 9, 13 }, { 9, 5, 7 }, { 5, 3, 4 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions3D_DifferentRatios()
		{ 
			// Different coarsening ratios on all three axes.
			var gridDimensions = GridDimensions.Create3D(numLevels: 5, numNodesFine: new[] { 811, 321, 1281 }, coarsenRatios: new[] { 3, 2, 4 });
			int[,] expected =
			{ 
				{ 811, 321, 1281 }, 
				{ 271, 161, 321 }, 
				{ 91, 81, 81 }, 
				{ 31, 41, 21 }, 
				{ 11, 21, 6 } 
			};
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions3D_TwoAxesEventuallyReachRatioOne()
		{
			// X has plenty of room for continued coarsening. Y and Z quickly reach the point where their coarsening ratio must be reduced and eventually becomes 1. 
			// X: 17 -> 9 -> 5 -> 3 -> 2 
			// Y: 5 -> 3 -> 2 -> 2 -> 2 
			// Z: 3 -> 2 -> 2 -> 2 -> 2
			var gridDimensions = GridDimensions.Create3D(numLevels: 5, numNodesFine: new[] { 17, 5, 3 }, coarsenRatios: new[] { 2, 2, 2 });
			int[,] expected = { { 17, 5, 3 }, { 9, 3, 2 }, { 5, 2, 2 }, { 3, 2, 2 }, { 2, 2, 2 } };
			AssertEqualDimensions(expected, gridDimensions);
		}

		[Fact]
		public static void TestGridDimensions3D_ThrowsWhenAllRatiosReachOneBeforeRequiredLevels()
		{
			var gridDimensions = GridDimensions.Create3D(numLevels: 5, numNodesFine: new[] { 17, 5, 3 }, coarsenRatios: new[] { 2, 2, 2 });
			Assert.ThrowsAny<Exception>(
				() => GridDimensions.Create3D(numLevels: 10, numNodesFine: new[] { 9, 9, 9 }, coarsenRatios: new[] { 2, 2, 2 })
			);
		}

		private static void AssertEqualDimensions(int[,] expected, GridDimensions gridDimensions)
		{
			var actual = new int[gridDimensions.NumLevels, gridDimensions.Dimension];
			for (int lvl = 0; lvl < gridDimensions.NumLevels; lvl++)
			{
				for (int d = 0; d < gridDimensions.Dimension; d++)
				{
					actual[lvl, d] = gridDimensions.GetNumNodesAtLevel(lvl)[d];
				}
			}

			Assert.Equal(expected, actual);
		}
	}
}
