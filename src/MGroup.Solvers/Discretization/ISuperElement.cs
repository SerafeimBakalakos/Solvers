namespace MGroup.Solvers.Discretization
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
	}
}
