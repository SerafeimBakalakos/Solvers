namespace MGroup.Solvers.Multigrid.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Commons;

	public class GalerkinProductCsr : IGalerkinProduct
	{
		private CsrMatrix prolongationCsr;
		private CscMatrix prolongationCsc;

		public IMatrix CalcProduct(IReadOnlyMatrix restriction, IReadOnlyMatrix fineGridMatrix, IReadOnlyMatrix prolongation)
		{
			if (restriction is not CsrMatrix)
			{
				throw new InvalidSparsityPatternException($"Restriction matrix must be a {nameof(CsrMatrix)}.");
			}
			if (fineGridMatrix is not CsrMatrix)
			{
				throw new InvalidSparsityPatternException($"Fine grid matrix matrix must be a {nameof(CsrMatrix)}.");
			}
			if (prolongation is not CsrMatrix)
			{
				throw new InvalidSparsityPatternException($"Prolongation matrix must be a {nameof(CsrMatrix)}.");
			}

			return CalcProduct((CsrMatrix)restriction, (CsrMatrix)fineGridMatrix, (CsrMatrix)prolongation);
		}

		public CsrMatrix CalcProduct(CsrMatrix restriction, CsrMatrix fineGridMatrix, CsrMatrix prolongation)
		{
			if (prolongation != this.prolongationCsr)
			{
				this.prolongationCsr = prolongation;
				this.prolongationCsc = Conversions.CsrToCsc(this.prolongationCsr);
			}

			CscMatrix temp = CsrCscMultiplications.CsrTimesCscToCsc(fineGridMatrix, prolongationCsc);
			return CsrCscMultiplications.CsrTimesCscToCsr(restriction, temp);
		}
	}
}
