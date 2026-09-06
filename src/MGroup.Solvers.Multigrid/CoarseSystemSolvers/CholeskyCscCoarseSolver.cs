namespace MGroup.Solvers.Multigrid.DirectSolver
{
	using System;
	using System.Collections.Generic;
	using System.Text;


	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Vectors;

	public class CholeskyCscCoarseSolver : ICoarseSystemSolver
	{
		private readonly IImplementationProvider provider;
		private readonly IReorderingAlgorithm reorderingAlgorithm;

		private ICholeskySymmetricCsc factorization;
		private Permutation permutation;
		private Permutation permutationInverse;
		private Vector rhsPermuted;
		private Vector solutionPermuted;

		public CholeskyCscCoarseSolver(IImplementationProvider provider, IReorderingAlgorithm? reorderingAlgorithm = null, double? factorizationTolerance = null)
		{
			this.provider = provider;
			
			if (reorderingAlgorithm is not null)
			{
				this.reorderingAlgorithm = reorderingAlgorithm;
			}
			else
			{
				this.reorderingAlgorithm = new AmdSymmetricOrdering(provider);
			}

			if (factorizationTolerance is not null)
			{
				throw new NotImplementedException();
			}
		}

		~CholeskyCscCoarseSolver()
		{
			ReleaseResources();
		}

		public void Dispose()
		{
			ReleaseResources();
			GC.SuppressFinalize(this);
		}

		public void Solve(Vector rhs, Vector solution)
		{
			rhs.PermuteIntoResult(permutation, rhsPermuted);
			factorization.SolveLinearSystem(rhsPermuted, solutionPermuted);
			solutionPermuted.PermuteIntoResult(permutationInverse, solution);
		}

		public void Update(IReadOnlyMatrix coarseMatrix, bool areDofsModified)
		{
			if (coarseMatrix is CsrMatrix csrMatrix)
			{
				SymmetricCscMatrix symCscMatrix = Conversions.CsrToSymmetricCsc(csrMatrix);

				// Reordering
				permutation = reorderingAlgorithm.FindPermutation(symCscMatrix.NumRows, symCscMatrix.RawRowIndices, symCscMatrix.RawColOffsets);
				permutationInverse = permutation.Invert();
				SymmetricCscMatrix reorderedMatrix = symCscMatrix.PermuteRowsAndCols(permutation);

				// Factorization
				factorization = provider.CreateCholeskyTriangulation(); //TODO: there must be a way to control the factorization tolerance
				factorization.Factorize(reorderedMatrix);

				// Work arrays/vectors
				rhsPermuted = Vector.CreateZero(coarseMatrix.NumRows);
				solutionPermuted = Vector.CreateZero(coarseMatrix.NumColumns);
			}
			else
			{
				throw new InvalidSparsityPatternException("The coarse linear system matrix must be in CSR format.");
			}
		}

		private void ReleaseResources()
		{
			if (factorization != null)
			{
				factorization.Dispose();
				factorization = null;
			}
		}
	}
}
