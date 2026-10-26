namespace MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;

	public class StandardFreeDofProvider : IFreeDofProvider
	{
		private readonly Model model;
		private readonly IDofType[] dofsPerNode;
		private readonly Func<ISubdomainFreeDofOrdering> getFreeDofOrdering;

		public StandardFreeDofProvider(Model model, IDofType[] dofsPerNode, Func<ISubdomainFreeDofOrdering> getFreeDofOrdering)
		{
			this.model = model;
			this.dofsPerNode = dofsPerNode;
			this.getFreeDofOrdering = getFreeDofOrdering;
		}

		public int[] FindFreeDofs()
		{
			ISubdomainFreeDofOrdering freeDofOrdering = getFreeDofOrdering();
			int numFreeDofs = freeDofOrdering.NumFreeDofs;
			IntDofTable freeDofs = freeDofOrdering.FreeDofs;
			ActiveDofs allDofs = freeDofOrdering.AllDofs;
			int numDofsPerNode = dofsPerNode.Length;

			var result = new int[numFreeDofs];
			foreach (Node node in model.EnumerateNodes())
			{
				for (int d = 0; d < numDofsPerNode; d++)
				{
					IDofType dof = dofsPerNode[d];
					if (freeDofs.TryGetValue(node.ID, allDofs.GetIdOfDof(dof), out int freeDofIdx))
					{
						result[freeDofIdx] = numDofsPerNode * node.ID + d;
					}
				}
			}

			return result;
		}
	}
}
