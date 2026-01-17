namespace MGroup.Solvers.DDM
{
	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;

	public interface ISubdomainTopology_v2
	{
		DistributedOverlappingIndexer CreateDistributedVectorIndexer(Func<int, IntDofTable> getSubdomainDofs);

		void FindCommonDofsBetweenSubdomains();

		void FindCommonNodesBetweenSubdomains();

		SortedSet<int> GetCommonNodesOfSubdomains(int localSubdomainID, int neighborSubdomainID);

		SortedSet<int> GetNeighborsOfSubdomain(int subdomainID);

		void Initialize(IComputeEnvironment environment, IPartition_v2 partition, Func<int, IMonolithicDofManager> getSubdomainFreeDofs);

		//DistributedOverlappingIndexer RecreateDistributedVectorIndexer(Func<int, IntDofTable> getSubdomainDofs,
		//	DistributedOverlappingIndexer previousIndexer, Func<int, bool> isModifiedSubdomain);

		//void RefindCommonDofsBetweenSubdomains(Func<int, bool> isModifiedSubdomain);
	}
}
