namespace MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg
{
	using System;

	using MGroup.LinearAlgebra.AlgebraicMultiGrid;
	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Iterative.Stationary.CSR;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.LinearAlgebra.Vectors;

	public class PodAmgPreconditioner : IPreconditioner
	{
		private readonly bool keepOnlyNonZeroPrincipalComponents;
		private readonly int numIterations;
		private readonly MultigridLevelSmoothing smoothing;

		private CsrMatrix fineMatrix;
		private CholeskyFull coarseMatrixFactorized;

		// Restriction is the transpose of this
		private Matrix prolongation;

		public PodAmgPreconditioner(
			bool keepOnlyNonZeroPrincipalComponents, MultigridLevelSmoothing smoothing, int numIterations)
		{
			this.keepOnlyNonZeroPrincipalComponents = keepOnlyNonZeroPrincipalComponents;
			this.smoothing = smoothing;
			this.numIterations = numIterations;
		}

		public IPreconditioner CopyWithInitialSettings()
			=> new PodAmgPreconditioner(keepOnlyNonZeroPrincipalComponents, smoothing.CopyWithInitialSettings(), numIterations);

		public void Initialize(Matrix sampleVectors, int numPrincipalComponents)
		{
			var pod = new ProperOrthogonalDecomposition(keepOnlyNonZeroPrincipalComponents);
			prolongation = pod.CalculatePrincipalComponents(sampleVectors.NumColumns, sampleVectors, numPrincipalComponents);
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
		{
			var rhs = (Vector)rhsVector;
			var solution = (Vector)lhsVector;
			solution.Clear();

			Preconditions.CheckSquareLinearSystemDimensions(fineMatrix, rhs, solution);
			var n0 = fineMatrix.NumRows;
			var r0 = Vector.CreateZero(n0);
			var e0 = Vector.CreateZero(n0);
			var n1 = coarseMatrixFactorized.Order;
			var r1 = Vector.CreateZero(n1);
			var e1 = Vector.CreateZero(n1);

			for (var i = 0; i < numIterations; i++)
			{
				// Pre-smoothing on lvl 0 to get an estimate of the solution x0. Use the x0 from previous cycles as initial guess.
				smoothing.ApplyPreSmoothers(rhs, solution);

				// Find the residual on lvl 0: r0=b-A0*x0
				//TODO: Use ExactResidual class for this
				fineMatrix.MultiplyIntoResult(solution, r0);
				r0.LinearCombinationIntoThis(-1.0, rhs, 1.0);

				// Restrict lvl 0 residual to lvl 1: r1 = P^T * r0
				prolongation.MultiplyIntoResult(r0, r1, transposeThis: true);

				// Find an estimate of the error on lvl 1 by solving exactly the system: A1*e1=r1.
				coarseMatrixFactorized.SolveLinearSystem(r1, e1);

				// Interpolate the lvl 1 error estimate to lvl 0: e0 = P * e1
				prolongation.MultiplyIntoResult(e1, e0, transposeThis: false);

				// Correct the solution estimate on lvl 0 using the interpolated error: x0 = x0 + e0
				solution.AddIntoThis(e0);

				// Post-smoothing on lvl 0 to further improve the solution estimate x0. Use the corrected x0 as initial guess.
				smoothing.ApplyPostSmoothers(rhs, solution);
			}
		}

		public void UpdateMatrix(IMatrixView matrix, bool isPatternModified)
		{
			if (prolongation == null)
			{
				throw new InvalidOperationException("This preconditioner must be initialized first");
			}

			if (matrix is CsrMatrix csrMatrix)
			{
				fineMatrix = csrMatrix;
				smoothing.UpdateMatrix(fineMatrix, true);
				Matrix temp = fineMatrix.MultiplyRight(prolongation);
				Matrix coarseMatrix = prolongation.MultiplyRight(temp, transposeThis: true, transposeOther: false);
				coarseMatrixFactorized = CholeskyFull.Factorize(coarseMatrix.NumRows, coarseMatrix.RawData);
			}
			else
			{
				throw new InvalidSparsityPatternException("This preconditioner can be used only for matrices in CSR format");
			}
		}
	}
}
