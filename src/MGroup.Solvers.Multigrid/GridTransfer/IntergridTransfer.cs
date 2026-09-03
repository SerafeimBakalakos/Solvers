namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	public class IntergridTransfer
	{
		private readonly IProlongationStrategy prolongationStrategy;
		private readonly IRestrictionStrategy restrictionStrategy;

		private CsrMatrix prolongation;
		private CsrMatrix restriction;

		public IntergridTransfer(IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy)
		{
			this.prolongationStrategy = prolongationStrategy;
			this.restrictionStrategy = restrictionStrategy;
		}

		public IReadOnlyMatrix Prolongation => prolongation;

		public IReadOnlyMatrix Restriction => restriction;

		public void Initialize(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			DokRowMajor prolongation = prolongationStrategy.CreateProlongationMatrix(numNodesFinePerAxis, numNodesCoarsePerAxis);
			DokRowMajor restriction = restrictionStrategy.CreateRestrictionMatrix(prolongation);

			this.prolongation = prolongation.BuildCsrMatrix(true);
			this.restriction = restriction.BuildCsrMatrix(true);
		}
	}
}
