//namespace MGroup.Solvers.DDM.PSM.InterfaceProblem
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;
//	using System.Threading.Tasks;

//	using MGroup.LinearAlgebra.Matrices;
//	using MGroup.LinearAlgebra.Vectors;
//	using MGroup.MSolve.Discretization.Entities;
//	using MGroup.Solvers;
//	using MGroup.Solvers.DDM.Partitioning;
//	using MGroup.Solvers.DDM.PSM.Dofs;
//	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
//	using MGroup.Solvers.DiscretizationExtensions;

//	public class PsmInterfaceProblemSuperElement : ISuperElement
//	{
//		private readonly PsmSubdomainDofs_v2 dofs;
//		private readonly IPsmSubdomainMatrixManager_v2 matrixManager;
//		private readonly IPartition_v2 partition;
//		private readonly ISubdomain_v2 subdomain;

//		public PsmInterfaceProblemSuperElement(IPartition_v2 partition, ISubdomain_v2 subdomain, PsmSubdomainDofs_v2 dofs, IPsmSubdomainMatrixManager_v2 matrixManager)
//		{
//			this.partition = partition;
//			this.subdomain = subdomain;
//			this.dofs = dofs;
//			this.matrixManager = matrixManager;
//		}

//		public int ID => subdomain.ID;

//		public IMatrix BuildMatrix()
//		{
//			return new SchurComplement_temp(dofs, matrixManager);
//		}

//		public IVector BuildRhsVector_temp() => throw new NotImplementedException();

//		public IntDofTable GetDofs()
//		{
//			return dofs.DofOrderingBoundary;
//		}

//		public IEnumerable<INode> GetNodes()
//		{
//			return subdomain.EnumerateNodes().Where(n => partition.FindMultiplicityOfNode(n.ID) > 1);
//		}
//	}
//}
