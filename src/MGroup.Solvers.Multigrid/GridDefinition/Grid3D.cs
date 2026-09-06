namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class Grid3D : IGrid
	{
		public Grid3D(int numNodesX, int numNodesY, int numNodesZ)
		{
			NumNodesPerAxis = new int[] { numNodesX, numNodesY, numNodesZ };
		}

		public Grid3D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length == 3) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0], numNodesPerAxis[1], numNodesPerAxis[2] };
		}

		public int Dimension => 3;

		public int[] NumNodesPerAxis { get; }
	}
}
