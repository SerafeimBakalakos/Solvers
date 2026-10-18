namespace MGroup.Solvers.Multigrid.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Logging;

	public class MultigridSolverLogger<THierarchyLog> : SolverLogger
		where THierarchyLog : MultigridOperatorHierarchyLog
	{
		private readonly List<THierarchyLog> operatorHierarchies = new ();

		public MultigridSolverLogger(string solverName)
			: base(solverName)
		{
		}

		public THierarchyLog GetOperatorHierarchy(int analysisStep) => operatorHierarchies[analysisStep];


		public void LogOperatorHierarchy(THierarchyLog hierarchyLog) => operatorHierarchies.Add(hierarchyLog);
	}
}
