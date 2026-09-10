namespace MGroup.Solvers.Multigrid.CoarseMatrix
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface ICoarseMatrixStrategy
	{
		void CalcCoarseSystemMatrices();
		
		void Clear();

		IReadOnlyMatrix GetLinearSystemMatrix(int lvl);
	}
}
