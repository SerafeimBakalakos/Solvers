namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class Prolongation3DVectorStrategy : IProlongationStrategy
	{
		private Prolongation3DScalarStrategy scalarProlongation;

		public Prolongation3DVectorStrategy(double tolerance = 1E-12)
		{
			scalarProlongation = new Prolongation3DScalarStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			var P = scalarProlongation.CreateProlongationMatrix(numNodesFinePerAxis, numNodesCoarsePerAxis);
			return P.KroneckerProductThisTimesIdentity(3);
		}
	}
}
