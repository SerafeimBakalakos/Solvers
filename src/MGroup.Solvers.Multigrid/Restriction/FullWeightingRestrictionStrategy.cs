namespace MGroup.Solvers.Multigrid.Restriction
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

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

		public CsrMatrix CalcRestrictionMatrix(CsrMatrix prolongation)
		{
			var coeff = 1.0 / Math.Pow(2, dimension);
			CsrMatrix restriction = prolongation.TransposeToCSR();
			restriction.ScaleIntoThis(coeff);
			return restriction;
		}
	}
}
