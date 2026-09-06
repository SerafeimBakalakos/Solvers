namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IGrid
	{
		/// <summary>
		/// 1D, 2D, or 3D
		/// </summary>
		int Dimension { get; }

		int[] NumNodesPerAxis {get;}
	}
}
