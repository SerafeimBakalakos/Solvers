namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class Prolongation2DVectorStrategy : IProlongationStrategy
	{
		private Prolongation2DScalarStrategy scalarProlongation;

		public Prolongation2DVectorStrategy(double tolerance = 1E-12)
		{
			scalarProlongation = new Prolongation2DScalarStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			var P = scalarProlongation.CreateProlongationMatrix(numNodesFinePerAxis, numNodesCoarsePerAxis);
			return P.KroneckerProductThisTimesIdentity(2);
		}
	}
}
