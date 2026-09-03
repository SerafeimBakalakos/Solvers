namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System.Diagnostics;

	using MGroup.LinearAlgebra.Vectors;

	/// <summary>
	/// Represents the Symmetric Gauss-Seidel, namely a forward GS: (D+L) * x(t+1) = b -U*x(t)),
	/// followed by a back SOR: (D+U) * x(t+1) = b -L*x(t).
	/// </summary>
	public class SgsIterationCsr : CsrStationaryIterationBase
	{
		public override string Name => "SSOR";

		public override IStationaryIteration CopyWithInitialSettings() => new SgsIterationCsr();

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null));
			provider.CsrGaussSeidelForward(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
				diagonalOffsets, rhs.RawData, solution.RawData);
			provider.CsrGaussSeidelBack(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
				diagonalOffsets, rhs.RawData, solution.RawData);
		}
	}
}
