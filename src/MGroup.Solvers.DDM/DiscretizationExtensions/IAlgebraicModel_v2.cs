namespace MGroup.Solvers.DDM.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public interface IAlgebraicModel_v2
	{
		ISubstructureProblem LinearSystem { get; }

		void AddToSubstructureVector(IEnumerable<INodalModelQuantity<IDofType>> nodalLoads, IVector vector);
	}
}
