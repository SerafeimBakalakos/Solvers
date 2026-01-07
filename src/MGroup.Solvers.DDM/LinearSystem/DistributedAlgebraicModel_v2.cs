namespace MGroup.Solvers.DDM.LinearSystem
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.Discretization;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Results;

	using MPI;

	public class DistributedAlgebraicModel_v2 : IAlgebraicModel_v2
	{
		private readonly IComputeEnvironment environment;
		private readonly LinearSystem_v2 linearSystem;
		private readonly IReadOnlyDictionary<int, ISubdomainDofOrdering_v2> freeDofOrderings;
		private readonly IModel_v2 model;
		private readonly IPartition_v2 partition;

		public DistributedAlgebraicModel_v2(IComputeEnvironment environment, IModel_v2 model, IPartition_v2 partition,
			LinearSystem_v2 linearSystem, IReadOnlyDictionary<int, ISubdomainDofOrdering_v2> freeDofOrderings)
		{
			this.environment = environment;
			this.model = model;
			this.partition = partition;
			this.linearSystem = linearSystem;
			this.freeDofOrderings = freeDofOrderings;
		}

		public void AddToGlobalVector(IEnumerable<INodalModelQuantity<IDofType>> nodalLoads, IVector vector)
		{
			DistributedOverlappingVector distributedVector = CheckCompatibleVector(vector);
			environment.DoPerNode(subdomainID =>
			{
				IntDofTable subdomainFreeDofs = freeDofOrderings[subdomainID].DomainDofs;
				var subdomainVector = distributedVector.LocalVectors[subdomainID];

				//TODO: This was optimized previously! ProblemStructural and Model provided only the loads that correspond to this subdomain
				foreach (INodalModelQuantity<IDofType> load in FilterSubdomainData(nodalLoads, subdomainID))
				{
					int dofID = model.DofTypes.GetIdOfDof(load.DOF);
					int dofIdx = subdomainFreeDofs[load.Node.ID, dofID];
					subdomainVector[dofIdx] += load.Amount;
				}

				//foreach (INodalModelQuantity<IDofType> nodalQuantity in nodalLoads)
				//{
				//	//TODO: This was optimized previously! ProblemStructural and Model provided only the loads that correspond to this subdomain
				//	if (partition.DoesSubdomainContainNode(nodalQuantity.Node.ID, subdomainID))
				//	{
				//		int dofID = model.DofTypes.GetIdOfDof(nodalQuantity.DOF);
				//		int dofIdx = subdomainFreeDofs[nodalQuantity.Node.ID, dofID];
				//		subdomainVector[dofIdx] += nodalQuantity.Amount;
				//	}
				//}
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
			IntDofTable subdomainFreeDofs = freeDofOrderings[subdomainID].DomainDofs;
			foreach ((int node, int dof, int freeDofIdx) in subdomainFreeDofs)
			{
				results[node, dof] = subdomainVector[freeDofIdx];
			}

			// Constrained dofs
			ActiveDofs activeDofs = model.DofTypes;
			var subdomain = (DefaultSubdomain_v2)partition.GetSubdomain(subdomainID);
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> constraints = subdomain.FindDiricletBCs();
			foreach (INodalDirichletBoundaryCondition<IDofType> constraint in constraints)
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

		private IEnumerable<Tbc> FilterSubdomainData<Tbc>(IEnumerable<Tbc> nodalBCs, int subdomainID)
			where Tbc : INodalModelQuantity<IDofType>
			=> nodalBCs.Where(bc => partition.DoesSubdomainContainNode(bc.Node.ID, subdomainID));
	}
}
