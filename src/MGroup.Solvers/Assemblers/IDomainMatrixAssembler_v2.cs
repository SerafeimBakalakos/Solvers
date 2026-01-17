namespace MGroup.Solvers.Assemblers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.Discretization;

	public interface IDomainMatrixAssembler_v2<TMatrix> where TMatrix : IMatrix
	{
		TMatrix BuildDomainMatrix(IDomain domain, IMonolithicDofManager dofManager);

		/// <summary>
		/// Update internal state when the freedom degree ordering is changed (e.g. reordering, XFEM, adaptive FEM). It 
		/// will be called after modifying the current freedom degree ordering.
		/// </summary>
		void HandleDofOrderingWasModified();
	}
}
