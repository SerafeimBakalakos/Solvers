namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public interface IRestrictionStrategy
	{
		DokRowMajor CreateRestrictionMatrix(DokRowMajor prolongation);
	}
}
