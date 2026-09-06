namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class Grid1D : IGrid
	{
		public Grid1D(int numNodes)
		{
			NumNodesPerAxis = new int[] { numNodes };
		}

		public Grid1D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length == 1) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0] };
		}

		public int Dimension => 1;

		public int[] NumNodesPerAxis { get; }
	}
}
