namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.GridDefinition;

	public interface IProlongationStrategy
	{
		DokRowMajor CreateProlongationMatrix(IGrid fineGrid, IGrid coarseGrid);
	}
}
