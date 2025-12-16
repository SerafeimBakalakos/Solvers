namespace MGroup.Solvers.DDM.SolversExtensions.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface ISubdomainMatrixAssembler<TMatrix> where TMatrix : IMatrix
	{
		IMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 subdomainDofOrdering);
	}
}
