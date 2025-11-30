namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;

	public class DefaultSubstructure_temp : ISubstructure
	{
		private readonly IModel model;

		public DefaultSubstructure_temp(ISubdomain subdomain, IModel model)
		{
			this.model = model;
			Subdomain = subdomain;
		}

		public ISubdomain Subdomain { get; }

		public IEnumerable<ISuperElement> EnumerateSuperElements()
		{
			ActiveDofs allDofs = model.GetActiveDofs_temp();
			foreach (IElementType element in Subdomain.EnumerateElements())
			{
				yield return new DefaultElement_temp(element, allDofs);
			}
		}

		public IntDofTable OrderDofs() => OrderFreeDofs(model, Subdomain.ID);

		protected static IntDofTable OrderFreeDofs(IModel model, int subdomainID)
		{
			var subdomain = model.GetSubdomain(subdomainID);
			ActiveDofs activeDofs = model.GetActiveDofs_temp();

			// Find all dofs
			var nodalDofTypesDictionary = new Dictionary<int, List<IDofType>>(); //TODO: use Set instead of List or a dedicated structure
			foreach (IElementType element in subdomain.EnumerateElements())
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
			IEnumerable<INodalBoundaryCondition<IDofType>> nodalBCs = model.EnumerateBoundaryConditions(subdomainID)
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(subdomain.EnumerateElements()))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
			foreach (INodalBoundaryCondition<IDofType> nodalBC in nodalBCs)
			{
				constrainedDofs.AddDof(nodalBC.Node, nodalBC.DOF);
			}

			// Order free dofs
			int dofIdx = 0;
			var freeDofs = new IntDofTable();
			foreach (INode node in subdomain.EnumerateNodes())
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
