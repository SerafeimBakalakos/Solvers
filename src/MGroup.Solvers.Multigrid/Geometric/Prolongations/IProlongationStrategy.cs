namespace MGroup.Solvers.Multigrid.Geometric.Prolongations
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Geometric.Grids;

	public interface IProlongationStrategy
	{
		DokRowMajor CreateProlongationMatrix(IUniformGrid fineGrid, IUniformGrid coarseGrid);
	}
}
