namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	public interface ISubdomain_v2
	{
		int ID { get; }

		IEnumerable<INode> EnumerateNodes();

		IEnumerable<ISuperElement> EnumerateElements();

		ISuperElement GetElement(int elementID);

		IntDofTable OrderDofs();
	}
}
