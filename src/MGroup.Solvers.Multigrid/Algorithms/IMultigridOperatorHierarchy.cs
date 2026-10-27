namespace MGroup.Solvers.Multigrid.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Logging;

	public interface IMultigridOperatorHierarchy
	{
		MultigridOperatorHierarchyLog Log { get; }

		public int NumLevels { get; }

		void BuildOperators(CsrMatrix finestLevelSystemMatrix);

		void Clear();

		CsrMatrix GetProlongation(int level);

		CsrMatrix GetRestriction(int level);

		CsrMatrix GetSystemMatrix(int level);
	}
}
