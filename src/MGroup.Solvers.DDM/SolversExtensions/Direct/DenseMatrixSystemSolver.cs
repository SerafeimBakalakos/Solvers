namespace MGroup.Solvers.DDM.SolversExtensions.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DenseMatrixSystemSolver : ISubstructureSolver
	{
		//private readonly reordering = new NullReordering(); Why do I need NullReordering? Solvers that do not need to reorder can just not call the ISubstructure.ReorderDofs() method

		public DenseMatrixSystemSolver(ISubstructure substructure)
		{
			this.Substructure = substructure;
		}

		public ISubstructureDofOrdering DofOrdering { get; set; }

		public IMatrix Matrix { get; set; }

		public IVector Rhs { get; set; }

		public IVector Solution { get; set; }

		public ISubstructure Substructure { get; set; }

		public void PrepareDofs()
		{
			DofOrdering.PrepareDofMaps();
			//DofOrdering.Reorder();
		}

		public void PrepareLinearSystem() => throw new NotImplementedException();

		public void SolveLinearSystem() => throw new NotImplementedException();
	}
}
