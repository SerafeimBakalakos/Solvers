namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class Prolongation2DScalarStrategy : IProlongationStrategy
	{
		private Prolongation1DStrategy prolongation1D;

		public Prolongation2DScalarStrategy(double tolerance = 1E-12)
		{
			prolongation1D = new Prolongation1DStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			GridPreconditions.CheckGrids2D(numNodesFinePerAxis, numNodesCoarsePerAxis);

			var nfx = numNodesFinePerAxis[0];
			var nfy = numNodesFinePerAxis[1];
			var ncx = numNodesCoarsePerAxis[0];
			var ncy = numNodesCoarsePerAxis[1];

			if (nfx > ncx && nfy > ncy)
			{
				var Px = prolongation1D.CreateProlongationMatrix([nfx], [ncx]);
				var Py = prolongation1D.CreateProlongationMatrix([nfy], [ncy]);
				return Py.KroneckerProduct(Px);
			}
			else if (nfx == ncx)
			{
				var Py = prolongation1D.CreateProlongationMatrix([nfy], [ncy]);
				return Py.KroneckerProductThisTimesIdentity(nfx);
			}
			else // nfy == ncy
			{
				var Px = prolongation1D.CreateProlongationMatrix([nfx], [ncx]);
				return Px.KroneckerProductIdentityTimesThis(nfy);
			}
		}
	}
}
