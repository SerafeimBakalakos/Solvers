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

	public interface ISubstructureMatrixAssembler<TMatrix> where TMatrix : IMatrix
	{
		IMatrix BuildSubstructureMatrix(ISubstructure substructure, ISubstructureDofOrdering substructureDofOrdering);
	}
}
