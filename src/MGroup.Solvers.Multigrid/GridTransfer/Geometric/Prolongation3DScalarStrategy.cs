namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class Prolongation3DScalarStrategy : IProlongationStrategy
	{
		private Prolongation1DStrategy prolongation1D;

		public Prolongation3DScalarStrategy(double tolerance = 1E-12)
		{
			prolongation1D = new Prolongation1DStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			//TODO: the order of operations matters: Pz*(Py*Px) vs (Pz*Py)*Px. Optimize it.
			GridPreconditions.CheckGrids3D(numNodesFinePerAxis, numNodesCoarsePerAxis);

			var nfx = numNodesFinePerAxis[0];
			var nfy = numNodesFinePerAxis[1];
			var nfz = numNodesFinePerAxis[2];
			var ncx = numNodesCoarsePerAxis[0];
			var ncy = numNodesCoarsePerAxis[1];
			var ncz = numNodesCoarsePerAxis[2];

			DokRowMajor P2D = null;
			var isP2DIdentity = false;
			if (nfx == ncx && nfy == ncy)
			{
				isP2DIdentity = true;
			}
			else if (nfx == ncx)
			{
				var Py = prolongation1D.CreateProlongationMatrix([nfy], [ncy]);
				P2D = Py.KroneckerProductThisTimesIdentity(nfx);
			}
			else if (nfy == ncy)
			{
				var Px = prolongation1D.CreateProlongationMatrix([nfx], [ncx]);
				P2D = Px.KroneckerProductIdentityTimesThis(nfy);
			}
			else // ((nfx > ncx) && (nfy > ncy))
			{
				var Px = prolongation1D.CreateProlongationMatrix([nfx], [ncx]);
				var Py = prolongation1D.CreateProlongationMatrix([nfy], [ncy]);
				P2D = Py.KroneckerProduct(Px);
			}

			if (isP2DIdentity) // Pz cannot be indentity too
			{
				var Pz = prolongation1D.CreateProlongationMatrix([nfz], [ncz]);
				return Pz.KroneckerProductThisTimesIdentity(nfx * nfy);
			}
			else
			{
				if (nfz == ncz)
				{
					return P2D.KroneckerProductIdentityTimesThis(nfz);
				}
				else
				{
					var Pz = prolongation1D.CreateProlongationMatrix([nfz], [ncz]);
					return Pz.KroneckerProduct(P2D);
				}
			}
		}
	}
}
