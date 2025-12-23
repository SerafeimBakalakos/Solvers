namespace MGroup.Solvers.LinearAlgebraExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public static class DiagonalMatrixExtensions
	{
		public static void AddSubmatrix(this DiagonalMatrix thisMatrix, int[] thisIndices, double[] submatrix)
		{
			double[] thisDiagonal = thisMatrix.RawDiagonal;
			for (int i = 0; i < thisIndices.Length; i++)
			{
				thisDiagonal[thisIndices[i]] += submatrix[i];
			}
		}

		public static void MultiplyIntoResult(this DiagonalMatrix thisMatrix, IReadOnlyVector lhs, IVector rhs)
		{
			if ((lhs is Vector lhsDense) && (rhs is Vector rhsDense))
			{
				thisMatrix.MultiplyIntoResult(lhsDense, rhsDense);
			}
			else
			{
				double[] thisDiagonal = thisMatrix.RawDiagonal;
				for (int i = 0; i < thisDiagonal.Length; i++)
				{
					rhs.Set(i, thisDiagonal[i] * lhs[i]);
				}
			}
		}

		public static void MultiplyIntoResult(this DiagonalMatrix thisMatrix, Vector lhs, Vector rhs)
		{
			Preconditions.CheckMultiplicationDimensionsMatrixVector(thisMatrix, lhs, rhs);

			//TODO: Do these with BLAS providers
			double[] thisDiagonal = thisMatrix.RawDiagonal;
			double[] x = lhs.RawData;
			double[] y = rhs.RawData;
			for (int i = 0; i < thisDiagonal.Length; i++)
			{
				y[i] = thisDiagonal[i] * x[i];
			}
		}
	}
}
