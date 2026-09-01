namespace MGroup.Solvers.Multigrid.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IGalerkinProduct
	{
		IMatrix CalcProduct(IReadOnlyMatrix restriction, IReadOnlyMatrix fineGridMatrix, IReadOnlyMatrix prolongation);
	}
}
