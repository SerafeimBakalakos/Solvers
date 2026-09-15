namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.GridDefinition;

	public class Prolongation2DVectorStrategy : IProlongationStrategy
	{
		private Prolongation2DScalarStrategy scalarProlongation;

		public Prolongation2DVectorStrategy(double tolerance = 1E-12)
		{
			scalarProlongation = new Prolongation2DScalarStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(IGrid fineGrid, IGrid coarseGrid)
		{
			var P = scalarProlongation.CreateProlongationMatrix(fineGrid, coarseGrid);
			return P.KroneckerProductThisTimesIdentity(2);
		}
	}
}
