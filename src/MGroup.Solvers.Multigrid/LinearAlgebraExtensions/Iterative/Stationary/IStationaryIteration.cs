namespace MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public interface IStationaryIteration : ISettingsCopiable<IStationaryIteration>
	{
		public string Name { get; }

		public void Execute(Vector rhs, Vector solution);

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified);
	}
}
