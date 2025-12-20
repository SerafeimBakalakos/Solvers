namespace MGroup.Solvers.DDM.PSM.Dofs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.Commons;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class PsmSubdomainDofs_v2
	{
		private readonly ISubdomain_v2 subdomain;
		private readonly ISubdomainDofOrdering_v2 dofOrdering;

		//TODO: This is essential for testing and very useful for debugging, but not production code. Should I remove it?
		private readonly bool sortDofsWhenPossible;

		public PsmSubdomainDofs_v2(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 dofOrdering, bool sortDofsWhenPossible = false)
		{
			this.subdomain = subdomain;
			this.dofOrdering = dofOrdering;
			this.sortDofsWhenPossible = sortDofsWhenPossible;
		}

		public IntDofTable DofOrderingBoundary { get; private set; }

		public int[] DofsBoundaryToAll { get; private set; }

		public int[] DofsInternalToAll { get; private set; }

		public void ReorderInternalDofs(DofPermutation permutation)
		{
			if (permutation.IsBetter)
			{
				DofsInternalToAll = permutation.ReorderKeysOfDofIndicesMap(DofsInternalToAll);
			}
		}

		/// <summary>
		/// Boundary/internal dofs
		/// </summary>
		public void SeparateDofsIntoBoundaryAndInternal()
		{
			//TODOMPI: force sorting per node and dof
			var boundaryDofOrdering = new IntDofTable();
			var boundaryToAll = new List<int>();
			var internalToAll = new HashSet<int>();
			int subdomainBoundaryIdx = 0;

			IntDofTable allDofs = dofOrdering.Dofs;
			IEnumerable<int> nodes = allDofs.GetRows();
			if (sortDofsWhenPossible)
			{
				nodes = nodes.OrderBy(node => node);
			}

			foreach (int node in nodes) //TODO: Optimize access: Directly get INode, Dictionary<IDof, int>
			{
				IReadOnlyDictionary<int, int> dofsOfNode = allDofs.GetDataOfRow(node);
				if (sortDofsWhenPossible)
				{
					var sortedDofsOfNode = new SortedDictionary<int, int>();
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						sortedDofsOfNode[dofTypeIdxPair.Key] = dofTypeIdxPair.Value;
					}
					dofsOfNode = sortedDofsOfNode;
				}

				if (subdomain.GetMultiplicityOfNode_temp(node) > 1)
				{
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						int dofID = dofTypeIdxPair.Key;
						boundaryDofOrdering[node, dofID] = subdomainBoundaryIdx++;
						boundaryToAll.Add(dofTypeIdxPair.Value);
					}
				}
				else
				{
					foreach (var dofTypeIdxPair in dofsOfNode)
					{
						internalToAll.Add(dofTypeIdxPair.Value);
					}
				}
			}

			this.DofOrderingBoundary = boundaryDofOrdering;
			this.DofsBoundaryToAll = boundaryToAll.ToArray();
			this.DofsInternalToAll = internalToAll.ToArray();
		}
	}
}
