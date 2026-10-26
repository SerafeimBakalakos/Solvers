namespace MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IFreeDofProvider
	{
		int[] FindFreeDofs();
	}
}
