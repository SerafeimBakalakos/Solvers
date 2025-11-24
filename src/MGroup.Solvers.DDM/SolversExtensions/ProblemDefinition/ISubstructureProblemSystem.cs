namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public interface ISubstructureProblemSystem
	{
		ISubstructureDofOrdering DofOrdering { get; set; }

		IMatrix Matrix { get; set; }

		IVector Rhs { get; set; }

		IVector Solution { get; set; }

		ISubstructure Substructure { get; set; }
	}
}
