namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Discretization;

	public interface IElementPartition
	{
		IEnumerable<ISuperElement> Elements { get; }

		int FindMultiplicityOfNode(int nodeID);

		void FindElementNeighbors();

		ISet<int> GetCommonNodesOfElements(int elementID0, int elementID1);

		IEnumerable<int> GetElementsOfNode(int nodeID);

		ISet<int> GetNeighborsOfElement(int elementID);
	}
}
