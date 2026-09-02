namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.Runtime.ConstrainedExecution;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Implementations.DotNumerics;

	using MGroup.MSolve.Discretization.Entities;

	/// <summary>
	/// Defines the number of nodes of 1D, 2D or 3D coarse grids for multigrid.
	/// </summary>
	public class GridDimensions
	{
		private readonly List<int[]> gridNodes;

		private GridDimensions(int dimension, int numLevels, int[] numNodesFine, int[] coarsenRatios)
		{
			CheckInput(dimension, numLevels, numNodesFine, coarsenRatios);

			Dimension = dimension;
			NumLevels = numLevels;

			// Define the number of nodes per axis for each grid. The top grid has the same number of nodes as the FEM model.
			gridNodes = new List<int[]>(numLevels);
			gridNodes.Add(CopyArray(numNodesFine));

			// For the rest, progressively decrease the number of nodes and possibly adjust the coarsening ratios
			var currentRatios = new int[dimension];
			Array.Copy(coarsenRatios, currentRatios, dimension);
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				var coarseNodes = new int[dimension];
				for (int d = 0; d < dimension; d++)
				{
					int nf = gridNodes[lvl - 1][d];
					int r = currentRatios[d];
					int nc = -1;
					while (r > 1) // Try to find a suitable ratio
					{
						// nf - 1 = fine elements. (nf - 1) / ratio = coarse elements
						if ((nf - 1) % r == 0)
						{
							nc = (nf - 1) / r + 1;
							if (nc >= 2) break; // Valid ratio. The search is over.
						}

						// Otherwise, we cannot coarsen this axis with the requested ratio any longer.
						// We will lower it and try again until the ratio is 1 (no coarsening).
						r = r - 1;
					}

					nc = (nf - 1) / r + 1; // Ensure the value is correct.
					coarseNodes[d] = nc;
					currentRatios[d] = r;
				}

				// Make sure the coarse grid is actually coarser, before storing it.
				// When all coarsening ratios become 1, we must stop.
				bool atLeastOneCoarsenedAxis = false;
				for (int d = 0; d < dimension; d++)
				{
					int nf = gridNodes[lvl - 1][d];
					int nc = coarseNodes[d];
					if (nf < nc) throw new Exception("This should not have happened.");
					else if (nf > nc) atLeastOneCoarsenedAxis = true;
				}

				if (atLeastOneCoarsenedAxis)
				{
					gridNodes.Add(coarseNodes);
				}
				else
				{
					throw new ArgumentException($"Cannot coarsen past level {lvl - 1}. Request fewer levels next time.");
				}
			}
		}

		/// <summary>
		/// The number of axes.
		/// </summary>
		public int Dimension { get; }

		/// <summary>
		/// Each level corresponds to one grid. Level 0 corresponds to the finest grid.
		/// </summary>
		public int NumLevels { get; }

		/// <summary>
		/// Generates the number of nodes of 1D coarse grids for multigrid.
		/// </summary>
		/// <param name="numLevels">How many grids/levels we want (including the finest).</param>
		/// <param name="numNodesFine">The number of nodes in the finest grid.</param>
		/// <param name="coarsenRatio">
		/// The coarsening ratio, ie how many fine elements fit inside one coarse element. 
		/// The given coarsening ratio may be lowered, when the requested ratio is no longer feasible.
		/// </param>
		/// <returns>A new instance of <see cref="GridDimensions"/></returns>
		public static GridDimensions Create1D(int numLevels, int numNodesFine, int coarsenRatio)
			=> new GridDimensions(1, numLevels, new int[] { numNodesFine }, new int[] { coarsenRatio });

		/// <summary>
		/// Generates the number of nodes of 2D coarse grids for multigrid.
		/// </summary>
		/// <param name="numLevels">How many grids/levels we want (including the finest).</param>
		/// <param name="numNodesFine">(1 x 2) array containing number of nodes in the finest grid along each axis.</param>
		/// <param name="coarsenRatios">
		/// (1 x 2) the coarsening ratio along each axis, ie how many fine elements fit inside one coarse element along that axis. 
		/// The given coarsening ratio along an axis may be lowered, when the requested ratio is no longer feasible along that axis.
		/// </param>
		/// <returns>A new instance of <see cref="GridDimensions"/></returns>
		public static GridDimensions Create2D(int numLevels, int[] numNodesFine, int[] coarsenRatios)
			=> new GridDimensions(2, numLevels, numNodesFine, coarsenRatios);

		/// <summary>
		/// Generates the number of nodes of 3D coarse grids for multigrid.
		/// </summary>
		/// <param name="numLevels">How many grids/levels we want (including the finest).</param>
		/// <param name="numNodesFine">(1 x 3) array containing number of nodes in the finest grid along each axis.</param>
		/// <param name="coarsenRatios">
		/// (1 x 3) the coarsening ratio along each axis, ie how many fine elements fit inside one coarse element along that axis. 
		/// The given coarsening ratio along an axis may be lowered, when the requested ratio is no longer feasible along that axis.
		/// </param>
		/// <returns>A new instance of <see cref="GridDimensions"/></returns>
		public static GridDimensions Create3D(int numLevels, int[] numNodesFine, int[] coarsenRatios)
			=> new GridDimensions(3, numLevels, numNodesFine, coarsenRatios);

		/// <summary>
		/// Returns the number of nodes per axis for the grid at <paramref name="level"/>.
		/// </summary>
		/// <param name="level">
		/// The level at which the grid is located at. Use 0 for the finest grid. Use <see cref="NumLevels-1"/> for the coarsest grid.
		/// </param>
		/// <returns>Array with the number of nodes along each axis.</returns>
		public int[] GetNumNodesAtLevel(int level) => gridNodes[level];

		private void CheckInput(int dimension, int numLevels, int[] numNodesFine, int[] coarsenRatios)
		{
			if ((dimension != 1) && (dimension != 2) && (dimension != 3))
			{
				throw new ArgumentException("The dimension must be 1, 2 or 3.");
			}

			if (numLevels < 2)
			{
				throw new ArgumentException("The number of levels must be integer >= 2.");
			}

			if (numNodesFine.Length != dimension)
			{
				throw new ArgumentException("The number of fine nodes must have as many entries as the dimension of the problem.");
			}

			if (coarsenRatios.Length != dimension)
			{
				throw new ArgumentException("The coarsening ratios must have as many entries as the dimension of the problem.");
			}

			int maxCoarsenRatio = -1;
			for (int d = 0; d < dimension; d++)
			{
				if (numNodesFine[d] < 2)
				{
					throw new ArgumentException($"The number of fine nodes at axis {d} must be integer >= 2.");
				}

				if (coarsenRatios[d] < 1)
				{
					throw new ArgumentException($"The coarsening ratio at axis {d} must be integer >= 1.");
				}

				if (coarsenRatios[d] > maxCoarsenRatio) maxCoarsenRatio = coarsenRatios[d];
			}

			if (maxCoarsenRatio <= 1)
			{
				throw new ArgumentException("There must be at least one axis along which the coarsening ratio is > 1.");
			}
		}

		private int[] CopyArray(int[] array)
		{
			var clone = new int[array.Length];
			Array.Copy(array, clone, array.Length);
			return clone;
		}
	}
}
