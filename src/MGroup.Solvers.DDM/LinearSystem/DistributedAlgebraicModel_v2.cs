namespace MGroup.Solvers.DDM.LinearSystem
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Results;

	using MPI;

	public class DistributedAlgebraicModel_v2 : IAlgebraicModel_v2
	{
		private readonly IComputeEnvironment environment;
		private readonly LinearSystem_v2 linearSystem;
		private readonly Dictionary<int, ISubdomainDofOrdering_v2> freeDofOrderings;
		private readonly IModel_v2 model;

		public DistributedAlgebraicModel_v2(IComputeEnvironment environment, IModel_v2 model, LinearSystem_v2 linearSystem, Dictionary<int, ISubdomainDofOrdering_v2> freeDofOrderings)
		{
			this.environment = environment;
			this.model = model;
			this.linearSystem = linearSystem;
			this.freeDofOrderings = freeDofOrderings;
		}

		public void AddToGlobalVector(IEnumerable<INodalModelQuantity<IDofType>> nodalLoads, IVector vector)
		{
			DistributedOverlappingVector distributedVector = CheckCompatibleVector(vector);
			environment.DoPerNode(subdomainID =>
			{
				IntDofTable subdomainFreeDofs = freeDofOrderings[subdomainID].Dofs;
				var subdomainVector = distributedVector.LocalVectors[subdomainID];

				foreach (INodalModelQuantity<IDofType> nodalQuantity in nodalLoads)
				{
					//TODO: This was optimized previously! ProblemStructural and Model provided only the loads that correspond to this subdomain
					if (nodalQuantity.Node.Subdomains.Contains(subdomainID))
					{
						int dofID = model.DofTypes.GetIdOfDof(nodalQuantity.DOF);
						int dofIdx = subdomainFreeDofs[nodalQuantity.Node.ID, dofID];
						subdomainVector[dofIdx] += nodalQuantity.Amount;
					}
				}
			});

			// Nodal loads at the same boundary dof are the same across all relevant subdomains, 
			// so we do not need to sum overlapping entries
		}

		public NodalResults ExtractAllResults(int subdomainID, IVector solutionFreeDofs)
		{
			var results = new Table<int, int, double>();

			// Free dofs
			DistributedOverlappingVector distributedVector = CheckCompatibleVector(solutionFreeDofs);
			Vector subdomainVector = distributedVector.LocalVectors[subdomainID];
			IntDofTable subdomainFreeDofs = freeDofOrderings[subdomainID].Dofs;
			foreach ((int node, int dof, int freeDofIdx) in subdomainFreeDofs)
			{
				results[node, dof] = subdomainVector[freeDofIdx];
			}

			// Constrained dofs
			ActiveDofs activeDofs = model.DofTypes;
			var constraints = model.FindDirichletBCsOfSubdomain(subdomainID);
			foreach (var constraint in constraints)
			{
				results[constraint.Node.ID, activeDofs.GetIdOfDof(constraint.DOF)] = constraint.Amount;
			}

			return new NodalResults(results);
		}

		internal DistributedOverlappingVector CheckCompatibleVector(IVector vector)
		{
			var rhs = (DistributedOverlappingVector)linearSystem.RhsVector;
			if (vector is DistributedOverlappingVector distributed)
			{
				if (rhs.HasSameFormat(distributed))
				{
					return distributed;
				}
			}

			throw new NonMatchingFormatException(
				"The provided vector has a different format than the current distributed linear system."
				+ $" Ensure it was created by this linear system object.");
		}
	}
}
