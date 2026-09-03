namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	public interface IProlongationStrategy
	{
		DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis);
	}
}
