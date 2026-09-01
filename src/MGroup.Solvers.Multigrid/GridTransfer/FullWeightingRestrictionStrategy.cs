namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class FullWeightingRestrictionStrategy : IRestrictionStrategy
	{
		private int dimension;

		public FullWeightingRestrictionStrategy(int dimension)
		{
			if (dimension != 1 && dimension != 2 && dimension != 3)
			{
				throw new ArgumentException("Dimension must be 1, 2 or 3");
			}

			this.dimension = dimension;
		}

		public DokRowMajor CreateRestrictionMatrix(DokRowMajor prolongation)
		{
			var coeff = 1.0 / Math.Pow(2, dimension);
			DokRowMajor restriction = prolongation.Transpose();
			restriction.ScaleIntoThis(coeff);
			return restriction;
		}
	}
}
