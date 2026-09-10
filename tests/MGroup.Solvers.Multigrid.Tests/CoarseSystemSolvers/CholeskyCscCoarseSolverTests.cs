namespace MGroup.Solvers.Multigrid.Tests.CoarseSystemSolvers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.DirectSolver;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class CholeskyCscCoarseSolverTests
	{
		[Fact]
		public static void TestLinearSystemSolution()
		{
			var matrix = CsrMatrix.CreateFromArrays(SparsePosDef10by10.Order, SparsePosDef10by10.Order,
				SparsePosDef10by10.CsrValues, SparsePosDef10by10.CsrColIndices, SparsePosDef10by10.CsrRowOffsets, true);
			var rhs = Vector.CreateFromArray(SparsePosDef10by10.Rhs);
			var solutionExpected = Vector.CreateFromArray(SparsePosDef10by10.Lhs);

			var provider = new ManagedSequentialImplementationProvider();
			var reordering = new AmdSymmetricOrdering(provider);
			using var solver = new CholeskyCscCoarseSolver(provider, reordering);
			solver.Initialize(matrix);

			var solutionComputed = Vector.CreateZero(rhs.Length);
			solver.Solve(rhs, solutionComputed);

			double tol = 1E-15;
			var comparer = new MatrixComparer(tol);
			comparer.AssertEqual(solutionExpected, solutionComputed);
		}
	}
}
