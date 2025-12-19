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

	public interface ISubdomainMatrixAssembler_v2<TMatrix> where TMatrix : IMatrix
	{
		TMatrix BuildSubdomainMatrix(ISubdomain_v2 subdomain, ISubdomainDofOrdering_v2 subdomainDofOrdering);

		/// <summary>
		/// Update internal state when the freedom degree ordering is changed (e.g. reordering, XFEM, adaptive FEM). It 
		/// will be called after modifying the current freedom degree ordering.
		/// </summary>
		void HandleDofOrderingWasModified();
	}
}
