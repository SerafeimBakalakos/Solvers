namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Vectors;

	/// <summary>
	/// Represents the Symmetric Successive Over-Relaxation, namely a forward SOR: (D+ωL) * x(t+1/2) = ω*(b -U*x(t)) +(1-ω)D*x(t),
	/// followed by a back SOR: (D+ωU) * x(t+1) = ω*(b -L*x(t+1/2)) +(1-ω)D*x(t+1/2).
	/// </summary>
	public class SsorIterationCsr : CsrStationaryIterationBase
	{
		private readonly double relaxationFactor;

		/// <summary>
		/// Initializes a new <see cref="SsorIterationCsr"/> with the specified settings.
		/// </summary>
		/// <param name="relaxationFactor">
		/// The scalar factor ω that enforces over-relaxation: x(t+1/2) = (1-ω)*x(t) + ω*x_FGS(t+1/2) and
		/// x(t+1) = (1-ω)*x(t+1/2) + ω*x_BGS(t+1), where x_FGS, x_BGS would be the forward and back Gauss-Seidel updates.
		/// </param>
		public SsorIterationCsr(double relaxationFactor)
		{
			this.relaxationFactor = relaxationFactor;
		}

		public override string Name => "SSOR";

		public override IStationaryIteration CopyWithInitialSettings() => new SsorIterationCsr(relaxationFactor);

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null));
			provider.CsrSorForward(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
				diagonalOffsets, rhs.RawData, solution.RawData, relaxationFactor);
			provider.CsrSorBack(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
				diagonalOffsets, rhs.RawData, solution.RawData, relaxationFactor);
		}
	}
}
