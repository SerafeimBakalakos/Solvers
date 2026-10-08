namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.TestData
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class Poisson1D_12Elements
	{
		public const double DomainLength = 1;

		public const int NumElements = 12;

		public const int SystemOrder = NumElements - 1;

		public const int SystemMatrixCsrNnz = 31;

		public static double[] SystemMatrixCsrValues 
			=> [24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24, -12, -12, 24];

		public static int[] SystemMatrixCsrColIndices 
			=> [0, 1, 0, 1, 2, 1, 2, 3, 2, 3, 4, 3, 4, 5, 4, 5, 6, 5, 6, 7, 6, 7, 8, 7, 8, 9, 8, 9, 10, 9, 10];

		public static int[] SystemMatrixCsrRowOffsets => [0, 2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 31];

		public static double[] SystemMatrixDiagonal => [24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24];

		public static double[] SystemMatrixNormalizedCsrValues
			=> [1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1, -0.5, -0.5, 1];

		public static readonly double SystemMatrixMaxEigenvalue = 2 * NumElements / DomainLength * (1.0 + Math.Cos(Math.PI / NumElements));

		public static readonly double SystemMatrixNormalizedMaxEigenvalue = SystemMatrixMaxEigenvalue / SystemMatrixCsrValues[0];

	}
}
