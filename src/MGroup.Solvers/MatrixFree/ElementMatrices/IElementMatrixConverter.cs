namespace MGroup.Solvers.MatrixFree.ElementMatrices
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	/// <summary>
	/// Converts the element matrices to a format that will be more efficient for repeated matrix-vector multiplications
	/// </summary>
	public interface IElementMatrixConverter
	{
		IMatrix ConvertElementMatrix(IMatrix original);
	}
}
