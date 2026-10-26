namespace MGroup.Solvers.Multigrid.Geometric.Grids
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Defines the number of nodes of 1D, 2D or 3D coarse grids for multigrid.
	/// </summary>
	public interface IGridHierarchy
	{
		/// <summary>
		/// The number of axes.
		/// </summary>
		public int Dimension { get; }

		/// <summary>
		/// Each level corresponds to one grid. Level 0 corresponds to the finest grid.
		/// </summary>
		public int NumLevels { get; }

		/// <summary>
		/// Calculate the exact grids that will be used.
		/// </summary>
		/// <param name="numLevelsRequested">
		/// How many grids to use, including the finerst. Must be > 1. It may not be possible to create all these levels. Check <see cref="NumLevels"/> for the actual count.
		/// </param>
		void DefineGrids(int numLevelsRequested);

		IUniformGrid GetGridAtLevel(int level);
	}
}
