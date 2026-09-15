namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.GridDefinition;

	public static class ProlongationUtilities
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

		public static void CheckGrids2D(IGrid fineGrid, IGrid coarseGrid)
		{
			if (fineGrid.Dimension != 2 || coarseGrid.Dimension != 2)
			{
				throw new ArgumentException("The fine and coarse grids must be 2-dimensional.");
			}

			int nfx = fineGrid.NumNodesPerAxis[0];
			int nfy = fineGrid.NumNodesPerAxis[1];
			int ncx = coarseGrid.NumNodesPerAxis[0];
			int ncy = coarseGrid.NumNodesPerAxis[1];

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

		public static void CheckGrids3D(IGrid fineGrid, IGrid coarseGrid)
		{
			if (fineGrid.Dimension != 3 || coarseGrid.Dimension != 3)
			{
				throw new ArgumentException("The number of nodes along all axes is mandatory.");
			}

			int nfx = fineGrid.NumNodesPerAxis[0];
			int nfy = fineGrid.NumNodesPerAxis[1];
			int nfz = fineGrid.NumNodesPerAxis[2];
			int ncx = coarseGrid.NumNodesPerAxis[0];
			int ncy = coarseGrid.NumNodesPerAxis[1];
			int ncz = coarseGrid.NumNodesPerAxis[2];

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

		/// <summary>
		/// Calculates the 2D prolongation matric as the tensor / Kronecker product of two 1D prolongation matrices in <see cref="DokRowMajor"/> format. 
		/// Each prolongation matrix corresponds to one axis and one axis is major, while the other is minor.
		/// Optimized for identity matrices.
		/// </summary>
		/// <param name="majorAxisP">1D prolongation matrix of the major axis. Use null, if it is identity.</param>
		/// <param name="numRowsMajorP">Number of rows of <paramref name="majorAxisP"/>"/>.</param>
		/// <param name="minorAxisP">Prolongation matrix of the minor axis. Use null, if it is identity.</param>
		/// <param name="numRowsMinorP">Number of rows of <paramref name="minorAxisP"/>.</param>
		/// <returns>
		/// The 2D prolongation matrix. Can be null (thus an identity matrix), if both <paramref name="majorAxisP"/> and <paramref name="minorAxisP"/> are null. 
		/// </returns>
		/// <exception cref="Exception"></exception>
		public static DokRowMajor? KroneckerProduct(DokRowMajor? majorAxisP, int numRowsMajorP, DokRowMajor? minorAxisP, int numRowsMinorP)
		{
			if ((majorAxisP != null) && (minorAxisP != null))
			{
				return minorAxisP.KroneckerProduct(majorAxisP);
			}
			else if ((majorAxisP == null) && (minorAxisP != null))
			{
				return minorAxisP.KroneckerProductThisTimesIdentity(numRowsMajorP);
			}
			else if ((majorAxisP != null) && (minorAxisP == null))
			{
				return majorAxisP.KroneckerProductIdentityTimesThis(numRowsMinorP);
			}
			else
			{
				return null;
			}
		}
	}
}
