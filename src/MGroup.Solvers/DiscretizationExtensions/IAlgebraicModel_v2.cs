namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.Results;

	public interface IAlgebraicModel_v2
	{
		void AddToGlobalVector(IEnumerable<INodalModelQuantity<IDofType>> nodalLoads, IVector vector);

		NodalResults ExtractAllResults(int subdomainID, IVector solutionFreeDofs);
	}
}
