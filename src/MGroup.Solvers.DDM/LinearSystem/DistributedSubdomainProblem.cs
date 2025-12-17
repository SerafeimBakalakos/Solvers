//namespace MGroup.Solvers.DDM.LinearSystem
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;
//	using System.Threading.Tasks;

//	using MGroup.LinearAlgebra.Matrices;
//	using MGroup.LinearAlgebra.Vectors;
//	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
//	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

//	public class DistributedSubdomainProblem : ISubdomainProblem
//	{
//		// Dofs = free dofs
//		public ISubdomainDofOrdering_v2 DofOrdering 

//		// Subdomain -> full domain
//		public ISubdomain_v2 Subdomain => throw new NotImplementedException();

//		public IMatrix SystemMatrix { get; set; }

//		public IVector SystemRhs { get; set; }

//		public IVector SystemSolution { get; set; }

//		public void OrderDofs() => throw new NotImplementedException();
//	}
//}
