namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;

	public class DefaultSubdomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly IElementMatrixProvider elementMatrixProvider;

		public DefaultSubdomain_temp(ISubdomain subdomain, IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
			Subdomain = subdomain;
		}

		public Dictionary<int, int> BoundaryDofMultiplicities = new Dictionary<int, int>();

		public ISubdomain Subdomain { get; }

		public IEnumerable<ISuperElement> EnumerateSuperElements()
		{
			foreach (IElementType element in Subdomain.EnumerateElements())
			{
				yield return new DefaultElement_temp(element, model.DofTypes, elementMatrixProvider);
			}
		}

		public virtual int GetMultiplicityOfNode(int nodeID)
		{
			if (BoundaryDofMultiplicities.TryGetValue(nodeID, out int multiplicity))
			{
				return multiplicity;
			}

			return 1;
		}

		public IntDofTable OrderDofs() => OrderFreeDofs(model, Subdomain.ID);

		protected static IntDofTable OrderFreeDofs(IModel_v2 model, int subdomainID)
		{
			var subdomain = model.GetSubdomain(subdomainID);
			ActiveDofs allDofs = model.DofTypes;

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
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> nodalDirichletBCs = model.FindDirichletBCsOfSubdomain(subdomainID);
			foreach (INodalBoundaryCondition<IDofType> nodalBC in nodalDirichletBCs)
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
						freeDofs[node.ID, allDofs.GetIdOfDof(dofType)] = dofIdx++;
					}
				}
			}

			return freeDofs;
		}
	}
}
