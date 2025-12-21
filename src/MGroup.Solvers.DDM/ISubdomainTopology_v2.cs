namespace MGroup.Solvers.DDM
{
	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.DofOrdering;

	public interface ISubdomainTopology_v2
	{
		DistributedOverlappingIndexer CreateDistributedVectorIndexer(Func<int, IntDofTable> getSubdomainDofs);

		void FindCommonDofsBetweenSubdomains();

		void FindCommonNodesBetweenSubdomains();

		SortedSet<int> GetCommonNodesOfSubdomains(int localSubdomainID, int neighborSubdomainID);

		SortedSet<int> GetNeighborsOfSubdomain(int subdomainID);

		void Initialize(IComputeEnvironment environment, IPartition_v2 partition, Func<int, ISubdomainDofOrdering_v2> getSubdomainFreeDofs);

		//DistributedOverlappingIndexer RecreateDistributedVectorIndexer(Func<int, IntDofTable> getSubdomainDofs,
		//	DistributedOverlappingIndexer previousIndexer, Func<int, bool> isModifiedSubdomain);

		//void RefindCommonDofsBetweenSubdomains(Func<int, bool> isModifiedSubdomain);
	}
}
