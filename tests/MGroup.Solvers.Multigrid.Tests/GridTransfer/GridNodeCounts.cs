namespace MGroup.Solvers.Multigrid.Tests.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class GridNodeCounts
	{
		private GridNodeCounts(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			this.Dimension = numNodesCoarsePerAxis.Length;
			NumNodesFinePerAxis = numNodesFinePerAxis;
			NumNodesCoarsePerAxis = numNodesCoarsePerAxis;
		}

		public int Dimension { get; }

		public int[] NumNodesFinePerAxis { get; }

		public int[] NumNodesCoarsePerAxis { get; }

		public static GridNodeCounts Create1D(int numNodesFine, int numNodesCoarse)
			=> new GridNodeCounts(new int[] { numNodesFine }, new int[] { numNodesCoarse });

		public static GridNodeCounts Create2D(int nfx, int nfy, int ncx, int ncy)
			=> new GridNodeCounts(new int[] { nfx, nfy }, new int[] { ncx, ncy });

		public static GridNodeCounts Create3D(int nfx, int nfy, int nfz, int ncx, int ncy, int ncz)
			=> new GridNodeCounts(new int[] { nfx, nfy, nfz }, new int[] { ncx, ncy, ncz });

		public bool Equals(GridNodeCounts other)
		{
			bool result = true;
			result &= other.NumNodesFinePerAxis.Length == this.NumNodesFinePerAxis.Length;
			result &= other.NumNodesCoarsePerAxis.Length == this.NumNodesCoarsePerAxis.Length;
			for (int d = 0; d < this.NumNodesCoarsePerAxis.Length;  d++)
			{
				result &= other.NumNodesFinePerAxis[d] == this.NumNodesFinePerAxis[d];
				result &= other.NumNodesCoarsePerAxis[d] == this.NumNodesCoarsePerAxis[d];
			}
			return result;
		}
	}
}
