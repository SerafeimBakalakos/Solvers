namespace MGroup.Solvers.DDM.SolversExtensions.LinearSystem
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.SolverExtensions.DofOrdering;

	public interface ISubstructureProblemSystem
	{
		ISubstructureDofOrdering DofOrdering { get; set; }

		IMatrix Matrix { get; set; }

		IVector Rhs { get; set; }

		IVector Solution { get; set; }

		ISubstructure Substructure { get; set; }
	}
}
