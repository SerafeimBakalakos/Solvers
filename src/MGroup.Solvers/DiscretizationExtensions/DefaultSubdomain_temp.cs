using MGroup.Solvers.DiscretizationExtensions;

namespace MGroup.Solvers.DiscretizationExtensions
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
	using MGroup.Solvers.DiscretizationExtensions;

	public class DefaultSubdomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly ISubdomain physicalSubdomain;
		private readonly IElementMatrixProvider elementMatrixProvider;

		public DefaultSubdomain_temp(ISubdomain subdomain, IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
			physicalSubdomain = subdomain;
		}

		public virtual IEnumerable<INode> EnumerateNodes_temp() => physicalSubdomain.EnumerateNodes();

		public IEnumerable<ISuperElement> EnumerateSuperElements()
		{
			foreach (IElementType element in physicalSubdomain.EnumerateElements())
			{
				yield return new DefaultElement_temp(element, model.DofTypes, elementMatrixProvider);
			}
		}

		public virtual int GetMultiplicityOfNode_temp(int nodeID)
		{
			return physicalSubdomain.GetMultiplicityOfNode(nodeID);
			//if (BoundaryDofMultiplicities.TryGetValue(nodeID, out int multiplicity))
			//{
			//	return multiplicity;
			//}

			//return 1;
		}

		public virtual ISubdomain_v2 GetSubdomain_temp(int subdomainID)
		{
			throw new Exception("Bottom level. This subdomain cannot be further divided");
		}

		public IntDofTable OrderDofs() => OrderFreeDofs(model, physicalSubdomain.ID);

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
