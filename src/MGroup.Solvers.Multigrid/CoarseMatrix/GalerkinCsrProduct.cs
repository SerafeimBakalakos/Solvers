namespace MGroup.Solvers.Multigrid.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons;

	public class GalerkinCsrProduct : IGalerkinProduct
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

			var restrictionCsr = (CsrMatrix)restriction;
			var fineGridMatrixCsr = (CsrMatrix)fineGridMatrix;
			
			if (prolongation != this.prolongationCsr)
			{
				this.prolongationCsr = (CsrMatrix)prolongation;
				this.prolongationCsc = Conversions.CsrToCsc(this.prolongationCsr);
			}

			return CalcProduct(restrictionCsr, fineGridMatrixCsr, prolongationCsc);
		}

		private CsrMatrix CalcProduct(CsrMatrix restriction, CsrMatrix fineMatrix, CscMatrix prolongation)
		{
			CscMatrix temp = CsrCscMultiplications.CsrTimesCscToCsc(fineMatrix, prolongation);
			return CsrCscMultiplications.CsrTimesCscToCsr(restriction, temp);
		}
	}
}
