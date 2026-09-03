namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;

	/// <summary>
	/// Represents a) the forward Gauss-Seidel (D+L) * x(t+1) = b -U*x(t)
	/// or b) the back Gauss-Seidel (D+U) * x(t+1) = b -L*x(t).
	/// The matrix A = D+L+U must be given in CSR format, where the entries of each row are sorted in ascending column order.
	/// </summary>
	/// <remarks>
	/// When using this iteration by itself repeatedly, convergence is guaranteed only for strictly diagonally dominant or
	/// positive definite (symmetric) matrices. Might converge in general matrix systems, as well, but with no guarantee.
	/// </remarks>
	public class GaussSeidelIterationCsr : CsrStationaryIterationBase
	{
		private readonly bool forwardDirection;

		public GaussSeidelIterationCsr(bool forwardDirection = true)
		{
			this.forwardDirection = forwardDirection;
		}

		public override string Name => forwardDirection ? "Forward Gauss-Seidel" : "Back Gauss-Seidel";

		public override IStationaryIteration CopyWithInitialSettings() => new GaussSeidelIterationCsr(forwardDirection);

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null));
			if (forwardDirection)
			{
				provider.CsrGaussSeidelForward(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData);
			}
			else
			{
				provider.CsrGaussSeidelBack(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData);
			}
		}
	}
}
