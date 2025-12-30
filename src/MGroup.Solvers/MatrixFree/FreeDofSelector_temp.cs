namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.Environments;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;

	using MPI;

	public class FreeDofSelector_temp
	{
		private readonly ISubdomain_v2 domain;
		private readonly IComputeEnvironment environment;
		private readonly IModel_v2 model;

		private Dictionary<int, Dictionary<int, int>> elementAllToFreeDofs = new Dictionary<int, Dictionary<int, int>>();
		private Dictionary<int, int[]> elementFreeToAllDofs = new Dictionary<int, int[]>();

		public FreeDofSelector_temp(IComputeEnvironment environment, ISubdomain_v2 domain)
		{
			this.environment = environment;
			this.model = ((FullDomain_temp)domain).Model;
			this.domain = domain;
		}

		public int[] FreeToAllDofsForElement(int elementID) => elementFreeToAllDofs[elementID];

		public IntDofTable GetFreeDofsOfElement(ISuperElement element)
		{
			var allDofs = element.GetDofs();
			Dictionary<int, int> allToFreeDofs = elementAllToFreeDofs[element.ID];

			var freeDofs = new IntDofTable();
			foreach ((int nodeID, int dofID, int dofIdx) in allDofs)
			{
				bool isFree = allToFreeDofs.TryGetValue(dofIdx, out int freeDofIdx);
				if (isFree)
				{
					freeDofs.TryAdd(nodeID, dofID, freeDofIdx);
				}
			}

			return freeDofs;
		}

		public void FindFreeDofs_temp()
		{
			// Find constrained dofs
			var constrainedDofs = new HashDofSet<INode, IDofType>();
			foreach (INodalBoundaryCondition<IDofType> nodalBC in model.GetDirichletBCs())
			{
				constrainedDofs.AddDof(nodalBC.Node, nodalBC.DOF);
			}

			environment.DoPerNode(elementID =>
			{
				IntDofTable elementDofs = domain.GetElement(elementID).GetDofs();
				var freeToAllDofs = new SortedSet<int>();
				foreach ((int nodeID, int dofID, int dofIdx) in elementDofs)
				{
					if (!constrainedDofs.Contains(model.GetNode(nodeID), model.DofTypes.GetDofWithId(dofID)))
					{
						freeToAllDofs.Add(dofIdx);
					}
				}
				
				StoreFreeDofs(elementID, freeToAllDofs.ToArray());
			});
		}

		private void StoreFreeDofs(int elementID, int[] freeToAllDofs)
		{
			elementFreeToAllDofs[elementID] = freeToAllDofs;

			var allToFreeDofs = new Dictionary<int, int>();
			for (int i = 0; i < freeToAllDofs.Length; i++)
			{
				allToFreeDofs[freeToAllDofs[i]] = i;
			}
			elementAllToFreeDofs[elementID] = allToFreeDofs;
		}
	}
}
