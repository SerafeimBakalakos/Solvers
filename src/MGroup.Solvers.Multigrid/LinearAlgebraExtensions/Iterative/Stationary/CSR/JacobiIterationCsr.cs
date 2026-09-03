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

	public class JacobiIterationCsr : CsrStationaryIterationBase
	{
		private double[]? workArray;

		public override string Name => "Jacobi";

		public override IStationaryIteration CopyWithInitialSettings() => new JacobiIterationCsr();

		public override void Execute(Vector rhs, Vector solution)
		{
			Debug.Assert((matrix is not null) && (diagonalOffsets is not null) && (workArray is not null));
			provider.CsrJacobi(matrix.NumRows, matrix.RawValues, matrix.RawRowOffsets, matrix.RawColIndices,
					diagonalOffsets, rhs.RawData, solution.RawData, workArray);
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
