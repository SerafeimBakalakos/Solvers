namespace MGroup.Solvers.MatrixFree.ElementMatrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class NullElementMatrixConverter : IElementMatrixConverter
	{
		public IMatrix ConvertElementMatrix(IMatrix original) => original;
	}
}
