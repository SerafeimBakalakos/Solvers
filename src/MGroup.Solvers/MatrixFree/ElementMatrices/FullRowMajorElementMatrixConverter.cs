namespace MGroup.Solvers.MatrixFree.ElementMatrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;

	public class FullRowMajorElementMatrixConverter : IElementMatrixConverter
	{
		public IMatrix ConvertElementMatrix(IMatrix original)
		{
			if (original is FullMatrixRowMajor)
			{
				return original;
			}
			else
			{
				return FullMatrixRowMajorExtensions.CreateFromMatrix(original);
			}
		}
	}
}
