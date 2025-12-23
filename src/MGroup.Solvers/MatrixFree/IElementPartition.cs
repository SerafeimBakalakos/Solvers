namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.DiscretizationExtensions;

	public interface IElementPartition
	{
		IEnumerable<ISuperElement> Elements { get; }

		int FindMultiplicityOfNode(int nodeID);
	}
}
