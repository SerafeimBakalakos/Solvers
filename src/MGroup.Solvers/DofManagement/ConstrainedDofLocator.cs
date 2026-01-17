namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;

	public class ConstrainedDofLocator
	{
		private readonly IModel_v2 model;
		private Dictionary<int, HashSet<int>> constrainedDofs;

		public ConstrainedDofLocator(IModel_v2 model)
		{
			this.model = model;
			FindConstrainedDofs();
		}

		public bool IsConstrainedDof(INode node, IDofType dof)
		{
			if (constrainedDofs.TryGetValue(node.ID, out HashSet<int> dofsOfNode))
			{
				return dofsOfNode.Contains(model.DofTypes.GetIdOfDof(dof));
			}

			return false;
		}

		private void FindConstrainedDofs()
		{
			constrainedDofs = new Dictionary<int, HashSet<int>>();
			foreach (INodalDirichletBoundaryCondition<IDofType> bc in model.GetDirichletBCs())
			{
				bool isNodeStored = constrainedDofs.TryGetValue(bc.Node.ID, out HashSet<int> dofsOfNode);
				if (!isNodeStored)
				{
					dofsOfNode = new HashSet<int>();
					constrainedDofs[bc.Node.ID] = dofsOfNode;
				}

				dofsOfNode.Add(model.DofTypes.GetIdOfDof(bc.DOF));
			}
		}
	}
}
