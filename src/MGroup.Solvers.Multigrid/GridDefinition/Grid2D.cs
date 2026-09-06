namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class Grid2D : IGrid
	{
		public Grid2D(int numNodesX, int numNodesY)
		{
			NumNodesPerAxis = new int[] { numNodesX, numNodesY };
		}

		public Grid2D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length == 2) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0], numNodesPerAxis[1] };
		}

		public int Dimension => 2;

		public int[] NumNodesPerAxis { get; }
	}
}
