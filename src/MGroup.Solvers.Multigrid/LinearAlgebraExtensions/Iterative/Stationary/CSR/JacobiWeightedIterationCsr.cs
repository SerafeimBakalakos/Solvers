namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;

	public class JacobiWeightedIterationCsr : CsrStationaryIterationBase
	{
		private readonly double relaxationFactor;
		private double[]? workArray;

		/// <summary>
		/// Initializes a new <see cref="JacobiWeightedIterationCsr"/> with the specified settings.
		/// </summary>
		/// <param name="relaxationFactor">
		/// The scalar factor ω that enforces over-relaxation: x(t+1) = (1-ω)*x(t) + ω*x_J(t), where x_J(t) would be the Jacobi update at iteration t.
		/// </param>
		public JacobiWeightedIterationCsr(double relaxationFactor)
		{
			this.relaxationFactor = relaxationFactor;
		}

		public override string Name => "Jacobi";

		public override IStationaryIteration CopyWithInitialSettings() => new JacobiWeightedIterationCsr(relaxationFactor);

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null) && (workArray is not null));
			provider.CsrJacobiWeighted(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData, relaxationFactor, workArray);
		}

		public override void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			base.UpdateMatrix(matrix, isPatternModified);
			if (isPatternModified || (workArray is null))
			{
				workArray = new double[matrix.NumColumns];
			}
		}
	}
}
