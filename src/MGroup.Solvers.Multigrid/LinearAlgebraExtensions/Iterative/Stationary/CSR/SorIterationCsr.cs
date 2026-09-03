namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;

	/// <summary>
	/// Represents a) the forward Successive Over-Relaxation (D+ωL) * x(t+1) = ω*(b -U*x(t)) +(1-ω)D*x(t)
	/// or b) the back SOR (D+ωU) * x(t+1) = ω*(b -L*x(t)) +(1-ω)D*x(t).
	/// The matrix A = D+L+U must be given in CSR format, where the entries of each row are sorted in ascending column order.
	/// </summary>
	/// <remarks>
	/// When using this iteration by itself repeatedly, convergence is guaranteed only for strictly diagonally dominant or
	/// positive definite (symmetric) matrices. Might converge in general matrix systems, as well, but with no guarantee.
	/// </remarks>
	public class SorIterationCsr : CsrStationaryIterationBase
	{
		private readonly double relaxationFactor;
		private readonly bool forwardDirection;

		/// <summary>
		/// Initializes a new <see cref="SorIterationCsr"/> with the specified settings.
		/// </summary>
		/// <param name="relaxationFactor">
		/// The scalar factor ω that enforces over-relaxation: x(t+1) = (1-ω)*x(t) + ω*x_GS(t), where x_GS(t) would be the Gauss-Seidel update at iteration t.
		/// </param>
		/// <param name="forwardDirection">
		/// True for forward SOR: (D+ωL) * x(t+1) = ω*(b -U*x(t)) +(1-ω)D*x(t).
		/// False for back SOR: (D+ωU) * x(t+1) = ω*(b -L*x(t)) +(1-ω)D*x(t).
		/// </param>
		public SorIterationCsr(double relaxationFactor, bool forwardDirection = true)
		{
			this.relaxationFactor = relaxationFactor;
			this.forwardDirection = forwardDirection;
		}

		public override string Name => forwardDirection ? "Forward SOR" : "Back SOR";

		public override IStationaryIteration CopyWithInitialSettings() 
			=> new SorIterationCsr(relaxationFactor, forwardDirection);

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null));
			if (forwardDirection)
			{
				provider.CsrSorForward(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData, relaxationFactor);
			}
			else
			{
				provider.CsrSorBack(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData, relaxationFactor);
			}
		}
	}
}
