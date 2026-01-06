namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	public interface ISuperElement
	{
		int ID { get; }

		IEnumerable<INode> EnumerateNodes();

		IntDofTable GetDofs();

		IMatrix BuildMatrix();

		IVector BuildRhsVector_temp();

		void PrepareDofs();
	}
}
