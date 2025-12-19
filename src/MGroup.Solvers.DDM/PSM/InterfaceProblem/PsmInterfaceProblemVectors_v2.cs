namespace MGroup.Solvers.DDM.PSM.InterfaceProblem
{
	using System.Collections.Generic;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.PSM.Vectors;

	public class PsmInterfaceProblemVectors_v2 : IPsmInterfaceProblemVectors
	{
		private const bool cacheDistributedVectorBuffers = true;
		private readonly IComputeEnvironment environment;
		private readonly IDictionary<int, PsmSubdomainVectors_v2> subdomainVectors;

		public PsmInterfaceProblemVectors_v2(IComputeEnvironment environment, IDictionary<int, PsmSubdomainVectors_v2> subdomainVectors)
		{
			this.environment = environment;
			this.subdomainVectors = subdomainVectors;
		}

		public DistributedOverlappingVector InterfaceProblemRhs { get; private set; }

		public DistributedOverlappingVector InterfaceProblemSolution { get; set; }

		// globalF = sum {Lb[s]^T * (fb[s] - Kbi[s] * inv(Kii[s]) * fi[s]) }
		public void CalcInterfaceRhsVector(DistributedOverlappingIndexer indexer)
		{
			Dictionary<int, Vector> fbCondensed = environment.CalcNodeData(
				subdomainID => subdomainVectors[subdomainID].CalcCondensedRhsVector());
			InterfaceProblemRhs = new DistributedOverlappingVector(indexer, fbCondensed);
			InterfaceProblemRhs.CacheSendRecvBuffers = cacheDistributedVectorBuffers;
			InterfaceProblemRhs.SumOverlappingEntries();
		}

		public void Clear()
		{
			InterfaceProblemRhs = null;
		}
	}
}
