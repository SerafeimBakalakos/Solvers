namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class StationaryIterationCandidatesRelaxation : INearNullspaceCandidatesRelaxationStrategy
	{
		private readonly int numSteps;
		private readonly IStationaryIteration stationaryIteration;

		public StationaryIterationCandidatesRelaxation(IStationaryIteration stationaryIteration, int numSteps)
		{
			this.numSteps = (numSteps > 1) ? numSteps : 1;
			this.stationaryIteration = stationaryIteration;
		}

		public void ImproveCandidateVectors(IReadOnlyMatrix systemMatrix, Matrix candidateVectors)
		{
			stationaryIteration.UpdateMatrix(systemMatrix, isPatternModified: true);
			var rhs = Vector.CreateZero(systemMatrix.NumRows);

			int numCandidates = candidateVectors.NumColumns;
			for (int c = 0; c < numCandidates; c++)
			{
				Vector candidate = candidateVectors.GetColumn(c);
				for (int i = 0; i < numSteps; i++)
				{
					stationaryIteration.Execute(rhs, candidate);
				}
				candidateVectors.SetSubcolumn(c, candidate, rowStart: 0);
			}
		}
	}
}
