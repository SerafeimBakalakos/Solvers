namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	public class IntergridTransfers
	{
		private readonly IProlongationStrategy prolongationStrategy;
		private readonly IRestrictionStrategy restrictionStrategy;

		private CsrMatrix[] prolongations = [];
		private CsrMatrix[] restrictions = [];

		public IntergridTransfers(IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy)
		{
			this.prolongationStrategy = prolongationStrategy;
			this.restrictionStrategy = restrictionStrategy;
		}

		public void Clear()
		{
			prolongations = [];
			restrictions = [];
		}

		public IReadOnlyMatrix GetProlongation(int level) => prolongations[level];

		public IReadOnlyMatrix GetRestriction(int level) => restrictions[level];

		public void Initialize(IGrid[] grids)
		{
			int numLevels = grids.Length;
			prolongations = new CsrMatrix[numLevels - 1];
			restrictions = new CsrMatrix[numLevels - 1];

			for (int lvl  = 0; lvl < numLevels - 1; lvl++)
			{
				int[] numNodesFine = grids[lvl].NumNodesPerAxis;
				int[] numNodesCoarse = grids[lvl + 1].NumNodesPerAxis;

				DokRowMajor prolongation = prolongationStrategy.CreateProlongationMatrix(numNodesFine, numNodesCoarse);
				DokRowMajor restriction = restrictionStrategy.CreateRestrictionMatrix(prolongation);
				prolongations[lvl] = prolongation.BuildCsrMatrix(true);
				restrictions[lvl] = restriction.BuildCsrMatrix(true);
			}
		}
	}
}
