namespace MGroup.Solvers.MatrixFree.ElementMatrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;

	public class CsrElementMatrixConverter
	{
		public IMatrix ConvertElementMatrix(IMatrix original)
		{
			if (original is CsrMatrix)
			{
				return original;
			}
			else
			{
				return CsrMatrix.CreateFromDense(original);
			}
		}
	}
}
