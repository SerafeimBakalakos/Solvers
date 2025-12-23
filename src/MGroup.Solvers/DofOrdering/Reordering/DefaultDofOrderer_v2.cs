namespace MGroup.Solvers.DofOrdering.Reordering
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;

	public class DefaultFreeDofOrderer_v2 : IFreeDofOrderer_v2
	{
		private readonly IModel_v2 model;

		public DefaultFreeDofOrderer_v2(IModel_v2 model)
		{
			this.model = model;
		}

		public IntDofTable OrderFreeDofs(IEnumerable<IElementType> elements, IEnumerable<INode> nodes, IEnumerable<INodalDirichletBoundaryCondition<IDofType>> nodalDirichletBCs)
		{
			ActiveDofs activeDofs = model.DofTypes;

			// Find all dofs
			var nodalDofTypesDictionary = new Dictionary<int, List<IDofType>>(); //TODO: use Set instead of List or a dedicated structure
			foreach (IElementType element in elements)
			{
				for (int i = 0; i < element.Nodes.Count; i++)
				{
					if (!nodalDofTypesDictionary.ContainsKey(element.Nodes[i].ID))
					{
						nodalDofTypesDictionary.Add(element.Nodes[i].ID, new List<IDofType>());
					}

					nodalDofTypesDictionary[element.Nodes[i].ID].AddRange(element.DofEnumerator.GetDofTypesForDofEnumeration(element)[i]);
				}
			}

			// Find constrained dofs
			var constrainedDofs = new HashDofSet<INode, IDofType>();
			foreach (INodalBoundaryCondition<IDofType> nodalBC in nodalDirichletBCs)
			{
				constrainedDofs.AddDof(nodalBC.Node, nodalBC.DOF);
			}

			// Order free dofs
			int dofIdx = 0;
			var freeDofs = new IntDofTable();
			foreach (INode node in nodes)
			{
				foreach (IDofType dofType in nodalDofTypesDictionary[node.ID].Distinct())
				{
					if (constrainedDofs.Contains(node, dofType) == false)
					{
						freeDofs[node.ID, activeDofs.GetIdOfDof(dofType)] = dofIdx++;
					}
				}
			}

			return freeDofs;
		}
	}
}
