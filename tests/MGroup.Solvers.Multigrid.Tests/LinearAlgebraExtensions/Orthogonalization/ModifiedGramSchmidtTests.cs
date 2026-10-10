namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Orthogonalization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Orthogonalization;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class ModifiedGramSchmidtTests
	{
		[Fact]
		public static void TestForIndependentColumns()
		{
			var matrixA = Matrix.CreateFromArray(new double[,]
			{
				{ 2, 1, -2,  0,  1,  3,  0, -1,  2,  1 },
				{ 0, 3,  1,  2,  0, -1,  2,  1,  0, -2 },
				{ 0, 0,  4, -1,  2,  0,  1,  0, -1,  2 },
				{ 0, 0,  0,  5, -2,  1,  0,  2,  1,  0 },
				{ 0, 0,  0,  0,  6,  2, -1,  0,  3,  1 },
				{ 0, 0,  0,  0,  0,  7,  3, -2,  0,  1 },
				{ 0, 0,  0,  0,  0,  0,  8,  1,  2, -1 },
				{ 0, 0,  0,  0,  0,  0,  0,  9, -1,  2 },
				{ 0, 0,  0,  0,  0,  0,  0,  0, 10,  3 },
				{ 0, 0,  0,  0,  0,  0,  0,  0,  0, 11 },
				{ 0, 0,  0,  0,  0,  0,  0,  0,  0,  0 },
				{ 0, 0,  0,  0,  0,  0,  0,  0,  0,  0 }
			});

			var expectedQ = Matrix.CreateFromArray(new double[,]
			{
				{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
			});

			var expectedR = Matrix.CreateFromArray(new double[,]
			{
				{ 2, 1, -2,  0,  1,  3,  0, -1,  2,  1 },
				{ 0, 3,  1,  2,  0, -1,  2,  1,  0, -2 },
				{ 0, 0,  4, -1,  2,  0,  1,  0, -1,  2 },
				{ 0, 0,  0,  5, -2,  1,  0,  2,  1,  0 },
				{ 0, 0,  0,  0,  6,  2, -1,  0,  3,  1 },
				{ 0, 0,  0,  0,  0,  7,  3, -2,  0,  1 },
				{ 0, 0,  0,  0,  0,  0,  8,  1,  2, -1 },
				{ 0, 0,  0,  0,  0,  0,  0,  9, -1,  2 },
				{ 0, 0,  0,  0,  0,  0,  0,  0, 10,  3 },
				{ 0, 0,  0,  0,  0,  0,  0,  0,  0, 11 }
			});

			var mgs = new ModifiedGramSchmidt();
			(Matrix computedQ, Matrix computedR) = mgs.Calculate(matrixA, tolerance: 1E-10);

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedQ, computedQ);
			comparer.AssertEqual(expectedR, computedR);
		}

		[Fact]
		public static void TestForDependentColumns()
		{
			var matrixA = Matrix.CreateFromArray(new double[,]
			{
				{ 2, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 3, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 2, 0, 1, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 2, 0, 2, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 3, 4, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
			});

			var expectedQ = Matrix.CreateFromArray(new double[,]
			{
				{ 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
			});

			var expectedR = Matrix.CreateFromArray(new double[,]
			{
				{ 2, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 3, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 2, 0, 1, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 2, 0, 2, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 3, 4, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0 },
				{ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5 }
			});

			var mgs = new ModifiedGramSchmidt();
			(Matrix computedQ, Matrix computedR) = mgs.Calculate(matrixA, tolerance: 1E-10);

			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedQ, computedQ);
			comparer.AssertEqual(expectedR, computedR);
		}
	}
}
