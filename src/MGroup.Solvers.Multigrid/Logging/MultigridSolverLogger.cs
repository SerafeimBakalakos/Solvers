namespace MGroup.Solvers.Multigrid.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.Solvers.Logging;

	public class MultigridSolverLogger : SolverLogger
	{
		protected readonly List<MultigridOperatorHierarchyLog> operatorHierarchies = new();

		public MultigridSolverLogger(string solverName)
			: base(solverName)
		{
		}

		public MultigridOperatorHierarchyLog GetOperatorHierarchy(int analysisStep) => operatorHierarchies[analysisStep];


		public void LogOperatorHierarchy(MultigridOperatorHierarchyLog hierarchyLog) => operatorHierarchies.Add(hierarchyLog);
	}
}
