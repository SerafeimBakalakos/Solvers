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
		private readonly IntergridTransfers intergridTransfers;
		private readonly int numLevels;

		private IReadOnlyMatrix[] allSystemMatrices;

		public GalerkinCoarseMatrixStrategy(int numLevels, IGalerkinProduct galerkinProduct, IntergridTransfers intergridTransfers, Func<IReadOnlyMatrix> getFineGridMatrix)
		{
			this.numLevels = numLevels;
			this.galerkinProduct = galerkinProduct;
			this.intergridTransfers = intergridTransfers;
			this.getFineGridMatrix = getFineGridMatrix;

			allSystemMatrices = new IReadOnlyMatrix[numLevels];
		}

		public void CalcCoarseSystemMatrices()
		{
			allSystemMatrices[0] = getFineGridMatrix();

			for (int lvl = 0; lvl < numLevels; lvl++)
			{
				IReadOnlyMatrix P = intergridTransfers.GetProlongation(lvl);
				IReadOnlyMatrix R = intergridTransfers.GetRestriction(lvl);
				IReadOnlyMatrix Kf = allSystemMatrices[lvl];
				allSystemMatrices[lvl + 1] = galerkinProduct.CalcProduct(R, Kf, P);
			}
		}
		
		public void Clear()
		{
			Array.Clear(allSystemMatrices, 0, allSystemMatrices.Length);
		}
		
		public IReadOnlyMatrix GetLinearSystemMatrix(int lvl) => allSystemMatrices[lvl];
	}
}
