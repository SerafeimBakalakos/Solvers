namespace MGroup.Solvers.Multigrid.Tests.Geometric.Grids
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.Geometric.Grids;

	using Xunit;

	public static class UniformGridHierarchyTests
	{
		[Fact]
		public static void Test1D_UniformRatio2()
		{
			// 17 nodes -> 9 -> 5 -> 3
			// With 4 levels, ratio 2 remains feasible at every step.
			var finestGrid = new UniformGrid1D(17);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 4);
			int[,] expected = { { 17 }, { 9 }, { 5 }, { 3 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test1D_RatioIsReducedWhenFullCoarseningIsNoLongerFeasible()
		{
			// 13 nodes = 12 elements: ratio 3 -> 5 nodes
			// 5 nodes = 4 elements: ratio 3 is no longer feasible
			// reduce ratio to 2 -> 3 nodes
			// 3 nodes = 2 elements: ratio 2 -> 2 nodes
			// The ratio remains 2 for the remaining levels.
			var finestGrid = new UniformGrid1D(13);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [3]);
			gridHierarchy.DefineGrids(numLevelsRequested: 4);
			int[,] expected = { { 13 }, { 5 }, { 3 }, { 2 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test2D_UniformRatio2()
		{
			// Both axes coarsen by 2 at every level.
			var finestGrid = new UniformGrid2D(9, 17);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 4);
			int[,] expected = { { 9, 17 }, { 5, 9 }, { 3, 5 }, { 2, 3 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test2D_DifferentRatios()
		{
			// X coarsens by 2, Y coarsens by 3.
			var finestGrid = new UniformGrid2D(17, 28);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 3]);
			gridHierarchy.DefineGrids(numLevelsRequested: 3);
			int[,] expected = { { 17, 28 }, { 9, 10 }, { 5, 4 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test2D_RatioOnOneAxisIsReducedToOne()
		{
			// X can continue coarsening by 2. Y eventually cannot, so its ratio is reduced and ultimately becomes 1. 
			// X: 17 -> 9 -> 5 -> 3 -> 2 
			// Y: 5 -> 3 -> 2 -> 2 -> 2
			var finestGrid = new UniformGrid2D(17, 5);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 5);
			int[,] expected = { { 17, 5 }, { 9, 3 }, { 5, 2 }, { 3, 2 }, { 2, 2 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact] 
		public static void Test2D_ReducesXRatioWhileYContinuesCoarsening() 
		{
			// X starts with ratio 4:
			// 25 nodes -> 7 nodes
			// 7 nodes: ratio 4 is not feasible, reduce to 3 -> 3 nodes
			// 3 nodes: ratio 3 is not feasible, reduce to 2 -> 2 nodes
			// 2 nodes: ratio 2 is not feasible, reduce to 1 -> 2 nodes

			// Y continues using ratio 2:
			// 65 -> 33 -> 17 -> 9 -> 5

			var finestGrid = new UniformGrid2D(25, 65);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [4, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 5);
			int[,] expected = { { 25, 65 }, { 7, 33 }, { 3, 17 }, { 2, 9 }, { 2, 5 } }; 
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test3D_UniformRatio2()
		{
			// All three axes coarsen by 2.
			var finestGrid = new UniformGrid3D(33, 17, 25);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 2, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 4);
			int[,] expected = { { 33, 17, 25 }, { 17, 9, 13 }, { 9, 5, 7 }, { 5, 3, 4 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test3D_DifferentRatios()
		{
			// Different coarsening ratios on all three axes.
			var finestGrid = new UniformGrid3D(811, 321, 1281);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [3, 2, 4]);
			gridHierarchy.DefineGrids(numLevelsRequested: 5);
			int[,] expected =
			{ 
				{ 811, 321, 1281 }, 
				{ 271, 161, 321 }, 
				{ 91, 81, 81 }, 
				{ 31, 41, 21 }, 
				{ 11, 21, 6 } 
			};
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test3D_TwoAxesEventuallyReachRatioOne()
		{
			// X has plenty of room for continued coarsening. Y and Z quickly reach the point where their coarsening ratio must be reduced and eventually becomes 1. 
			// X: 17 -> 9 -> 5 -> 3 -> 2 
			// Y: 5 -> 3 -> 2 -> 2 -> 2 
			// Z: 3 -> 2 -> 2 -> 2 -> 2
			var finestGrid = new UniformGrid3D(17, 5, 3);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 2, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 5);
			int[,] expected = { { 17, 5, 3 }, { 9, 3, 2 }, { 5, 2, 2 }, { 3, 2, 2 }, { 2, 2, 2 } };
			AssertEqualNodes(expected, gridHierarchy);
		}

		[Fact]
		public static void Test3D_CannotCreateAllRequestedLevels()
		{
			var finestGrid = new UniformGrid3D(9, 9, 9);
			var gridHierarchy = new UniformGridHierarchy(finestGrid, targetCoarseningRatios: [2, 2, 2]);
			gridHierarchy.DefineGrids(numLevelsRequested: 10);
			Assert.Equal(4, gridHierarchy.NumLevels);
		}

		private static void AssertEqualNodes(int[,] expected, UniformGridHierarchy hierarchy)
		{
			var actual = new int[hierarchy.NumLevels, hierarchy.Dimension];
			for (int lvl = 0; lvl < hierarchy.NumLevels; lvl++)
			{
				for (int d = 0; d < hierarchy.Dimension; d++)
				{
					actual[lvl, d] = hierarchy.GetNumNodesAtLevel(lvl)[d];
				}
			}

			Assert.Equal(expected, actual);
		}
	}
}
