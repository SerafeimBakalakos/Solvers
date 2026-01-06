//namespace MGroup.Solvers.DDM.PSM.InterfaceProblem
//{
//	using System;
//	using System.Collections.Concurrent;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;
//	using System.Threading.Tasks;

//	using MGroup.Environments;
//	using MGroup.MSolve.Discretization.Entities;
//	using MGroup.Solvers;
//	using MGroup.Solvers.DDM.Partitioning;
//	using MGroup.Solvers.DDM.PSM.Dofs;
//	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
//	using MGroup.Solvers.DiscretizationExtensions;

//	public class PsmInterfaceProblem_v2 : ISubdomain_v2
//	{
//		private readonly IComputeEnvironment environment;
//		private readonly IPartition_v2 partition;
//		private readonly IReadOnlyDictionary<int, PsmSubdomainDofs_v2> subdomainDofs;
//		private readonly IReadOnlyDictionary<int, IPsmSubdomainMatrixManager_v2> subdomainMatrices;
//		private readonly ISubdomainTopology_v2 subdomainTopology;

//		private ConcurrentDictionary<int, PsmInterfaceProblemSuperElement> superElements;

//		public PsmInterfaceProblem_v2(IComputeEnvironment environment, IPartition_v2 partition, ISubdomainTopology_v2 subdomainTopology,
//			IReadOnlyDictionary<int, PsmSubdomainDofs_v2> subdomainDofs,
//			IReadOnlyDictionary<int, IPsmSubdomainMatrixManager_v2> subdomainMatrices)
//		{
//			this.environment = environment;
//			this.partition = partition;
//			this.subdomainTopology = subdomainTopology;
//			this.subdomainDofs = subdomainDofs;
//			this.subdomainMatrices = subdomainMatrices;

//			this.superElements = new ConcurrentDictionary<int, PsmInterfaceProblemSuperElement>();
//			environment.DoPerNode(subdomainID =>
//			{
//				superElements[subdomainID] = new PsmInterfaceProblemSuperElement(partition,
//					partition.GetSubdomain(subdomainID),
//					subdomainDofs[subdomainID],
//					subdomainMatrices[subdomainID]);
//			});
//		}

//		public int ID => 0;

//		public IEnumerable<ISuperElement> EnumerateElements() => superElements.Values;

//		public IEnumerable<INode> EnumerateNodes() => throw new NotImplementedException();
//	}
//}
