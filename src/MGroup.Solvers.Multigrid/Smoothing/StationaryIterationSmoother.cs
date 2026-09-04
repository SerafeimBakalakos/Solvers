namespace MGroup.Solvers.Multigrid.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;

	public class StationaryIterationSmoother : IMultigridSmoother
	{
		private readonly int numSteps;
		private readonly IStationaryIteration stationaryIteration;

		public StationaryIterationSmoother(IStationaryIteration stationaryIteration, int numSteps)
		{
			this.numSteps = (numSteps > 1) ? numSteps : 1;
			this.stationaryIteration = stationaryIteration;
		}

		public void Apply(Vector rhs, Vector solution)
		{
			for (int i = 0; i < numSteps; i++)
			{
				stationaryIteration.Execute(rhs, solution);
			}
		}

		public IMultigridSmoother DeepCopy() 
			=> new StationaryIterationSmoother(stationaryIteration.CopyWithInitialSettings(), numSteps);

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool areDofsModified)
		{
			stationaryIteration.UpdateMatrix(matrix, areDofsModified);
		}
	}
}
