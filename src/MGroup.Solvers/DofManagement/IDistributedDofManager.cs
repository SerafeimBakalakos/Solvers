namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Distributed.Overlapping;

	public interface IDistributedDofManager : IDomainDofManager
	{
		DistributedOverlappingIndexer DistributedIndexer { get; }
	}
}
