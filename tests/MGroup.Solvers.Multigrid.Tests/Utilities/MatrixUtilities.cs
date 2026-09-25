namespace MGroup.Solvers.Multigrid.Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class MatrixUtilities
	{
		public static CsrMatrix ArrayToCsr(double[,] matrix)
		{
			DokRowMajor dok = ArrayToDok(matrix);
			return dok.BuildCsrMatrix(sortColsOfEachRow: true);
		}

		public static DokRowMajor ArrayToDok(double[,] matrix)
		{
			int numRows = matrix.GetLength(0);
			int numCols = matrix.GetLength(1);
			var dok = DokRowMajor.CreateEmpty(numRows, numCols);

			for (int i = 0; i < numRows; i++)
			{
				for (int j = 0; j < numCols; j++)
				{
					if (matrix[i, j] != 0)
					{
						dok[i, j] = matrix[i, j];
					}
				}
			}

			return dok;
		}
		public static double[,] CreateIdentityAsArray(int order)
		{
			var result = new double[order, order];
			for (int i = 0; i < order; i++)
			{
				result[i, i] = 1.0;
			}

			return result;
		}
	}
}
