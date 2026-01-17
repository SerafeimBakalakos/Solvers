namespace MGroup.Solvers.Discretization
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Entities;

	public interface IDomain
	{
		public int NumElements { get; }

		public int NumNodes { get; }

		IEnumerable<ISuperElement> EnumerateElements();

		IEnumerable<INode> EnumerateNodes();

		ISuperElement GetElement(int elementID);

		INode GetNode(int nodeID);
	}
}
