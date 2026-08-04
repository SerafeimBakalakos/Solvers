namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public static class GridPreconditions
	{
		public static void CheckGrids1D(int numNodesFine, int numNodesCoarse)
		{
			if (numNodesFine < 2 || numNodesCoarse < 2)
			{
				throw new ArgumentException("The number of nodes at each mesh must be >= 2.");
			}

			if (numNodesFine <= numNodesCoarse)
			{
				throw new ArgumentException("In order to coarsen, the number of nodes in the fine grid must be greater than the number of nodes in the coarse grid.");
			}

			if ((numNodesFine - 1) % (numNodesCoarse - 1) != 0)
			{
				throw new ArgumentException("(numNodesFine-1) mod (numNodesCoarse-1) must be 0, in order for coarse nodes to coincide with fine nodes.");
			}
		}

		public static void CheckGrids2D(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			if (numNodesFinePerAxis.Length != 2 || numNodesCoarsePerAxis.Length != 2)
			{
				throw new ArgumentException("The number of nodes along all axes is mandatory.");
			}

			var nfx = numNodesFinePerAxis[0];
			var nfy = numNodesFinePerAxis[1];
			var ncx = numNodesCoarsePerAxis[0];
			var ncy = numNodesCoarsePerAxis[1];

			if (nfx < 2 || nfy < 2 || ncx < 2 || ncy < 2)
			{
				throw new ArgumentException("The number of nodes along each axis of any grid must be >= 2.");
			}

			if (nfx < ncx || nfy < ncy)
			{
				throw new ArgumentException("There cannot be fewer nodes in the fine grid, compared to the coarse grid, along any axis.");
			}

			if (nfx == ncx && nfy == ncy)
			{
				throw new ArgumentException("There must be fewer nodes in the coarse grid along one axis at least, in order to coarsen.");
			}

			if ((nfx - 1) % (ncx - 1) != 0)
			{
				throw new ArgumentException("(nfx-1) mod (ncx-1) must be 0, in order for coarse nodes to coincide with fine nodes along axis x.");
			}

			if ((nfy - 1) % (ncy - 1) != 0)
			{
				throw new ArgumentException("(nfy-1) mod (ncy-1) must be 0, in order for coarse nodes to coincide with fine nodes along axis y.");
			}
		}

		public static void CheckGrids3D(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			if (numNodesFinePerAxis.Length != 3 || numNodesCoarsePerAxis.Length != 3)
			{
				throw new ArgumentException("The number of nodes along all axes is mandatory.");
			}

			var nfx = numNodesFinePerAxis[0];
			var nfy = numNodesFinePerAxis[1];
			var nfz = numNodesFinePerAxis[2];
			var ncx = numNodesCoarsePerAxis[0];
			var ncy = numNodesCoarsePerAxis[1];
			var ncz = numNodesCoarsePerAxis[2];

			if (nfx < 2 || nfy < 2 || nfz < 2 || ncx < 2 || ncy < 2 || ncz < 2)
			{
				throw new ArgumentException("The number of nodes along each axis of any grid must be >= 2.");
			}

			if (nfx < ncx || nfy < ncy || nfz < ncz)
			{
				throw new ArgumentException("There cannot be fewer nodes in the fine grid, compared to the coarse grid, along any axis.");
			}

			if (nfx == ncx && nfy == ncy && nfz == ncz)
			{
				throw new ArgumentException("There must be fewer nodes in the coarse grid along one axis at least, in order to coarsen.");
			}

			if ((nfx - 1) % (ncx - 1) != 0)
			{
				throw new ArgumentException("(nfx-1) mod (ncx-1) must be 0, in order for coarse nodes to coincide with fine nodes along axis x.");
			}

			if ((nfy - 1) % (ncy - 1) != 0)
			{
				throw new ArgumentException("(nfy-1) mod (ncy-1) must be 0, in order for coarse nodes to coincide with fine nodes along axis y.");
			}

			if ((nfz - 1) % (ncz - 1) != 0)
			{
				throw new ArgumentException("(nfz-1) mod (ncz-1) must be 0, in order for coarse nodes to coincide with fine nodes along axis z.");
			}
		}
	}
}
