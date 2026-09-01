namespace MGroup.Solvers.Multigrid.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.GridTransfer;

	public class GalerkinCoarseMatrixStrategy : ICoarseMatrixStrategy
	{
		private readonly IGalerkinProduct galerkinProduct;
		private readonly Func<IReadOnlyMatrix> getFineGridMatrix;
		private readonly IntergridTransfer intergridTransfer;

		public GalerkinCoarseMatrixStrategy(IGalerkinProduct galerkinProduct, IntergridTransfer intergridTransfer, Func<IReadOnlyMatrix> getFineGridMatrix)
		{
			this.galerkinProduct = galerkinProduct;
			this.intergridTransfer = intergridTransfer;
			this.getFineGridMatrix = getFineGridMatrix;
		}

		public IMatrix CalcCoarseGridMatrix()
		{
			IReadOnlyMatrix P = intergridTransfer.Prolongation;
			IReadOnlyMatrix R = intergridTransfer.Restriction;
			IReadOnlyMatrix Kf = getFineGridMatrix();
			return galerkinProduct.CalcProduct(R, Kf, P);
		}
	}
}
