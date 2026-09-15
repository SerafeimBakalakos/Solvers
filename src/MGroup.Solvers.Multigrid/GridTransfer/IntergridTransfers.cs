namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public class IntergridTransfers
	{
		private readonly IDofType[] dofsPerNode;
		private readonly Model modelFinest;
		private readonly IProlongationStrategy prolongationStrategy;
		private readonly IRestrictionStrategy restrictionStrategy;

		private List<int[]> freeDofMaps = new();
		private CsrMatrix[] prolongations = [];
		private CsrMatrix[] restrictions = [];

		public IntergridTransfers(Model modelFinest, IDofType[] dofsPerNode, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy)
		{
			this.prolongationStrategy = prolongationStrategy;
			this.restrictionStrategy = restrictionStrategy;
			this.modelFinest = modelFinest;
			this.dofsPerNode = dofsPerNode;
		}

		public void Clear()
		{
			freeDofMaps.Clear();
			prolongations = [];
			restrictions = [];
		}

		public IReadOnlyMatrix GetProlongation(int level) => prolongations[level];

		public IReadOnlyMatrix GetRestriction(int level) => restrictions[level];

		public void Initialize(IGrid[] grids, ISubdomainFreeDofOrdering freeDofOrderingFinestGrid, ActiveDofs allDofs)
		{
			int numLevels = grids.Length;
			prolongations = new CsrMatrix[numLevels - 1];
			restrictions = new CsrMatrix[numLevels - 1];
			freeDofMaps.Capacity = numLevels;
			freeDofMaps.Add(FindFreeDofsFinest(freeDofOrderingFinestGrid, allDofs));

			for (int lvl  = 0; lvl < numLevels - 1; lvl++)
			{
				DokRowMajor prolongation = prolongationStrategy.CreateProlongationMatrix(grids[lvl], grids[lvl + 1]);
				int[] freeDofsFine = freeDofMaps[lvl];
				int[] freeDofsCoarse = FindFreeDofsCoarse(prolongation, freeDofsFine);
				freeDofMaps.Add(freeDofsCoarse);

				DokRowMajor prolongationFree = prolongation.GetSubmatrix(freeDofsFine, freeDofsCoarse);
				prolongations[lvl] = prolongationFree.BuildCsrMatrix(true);

				DokRowMajor restrictionFree = restrictionStrategy.CreateRestrictionMatrix(prolongationFree);
				restrictions[lvl] = restrictionFree.BuildCsrMatrix(true);
			}
		}

		private int[] FindFreeDofsCoarse(DokRowMajor prolongation, int[] freeDofsFine)
		{
			DokRowMajor_SubmatrixView prolongationFreeAll = prolongation.ViewSubmatrix(freeDofsFine);
			int[] nnzOfColumns = prolongationFreeAll.CountNonZerosOfColumns();
			int numColumns = prolongation.NumColumns;
			var freeDofsCoarse = new List<int>(numColumns);
			for (int col = 0; col < numColumns; col++)
			{
				if (nnzOfColumns[col] > 0)
				{
					freeDofsCoarse.Add(col);
				}
			}
			return freeDofsCoarse.ToArray();
		}

		private int[] FindFreeDofsFinest(ISubdomainFreeDofOrdering freeDofOrderingFinestGrid, ActiveDofs allDofs)
		{
			int numDofsPerNode = dofsPerNode.Length;
			int numFreeDofs = freeDofOrderingFinestGrid.NumFreeDofs;
			IntDofTable freeDofs = freeDofOrderingFinestGrid.FreeDofs;
			var result = new int[numFreeDofs];

			foreach (Node node in modelFinest.EnumerateNodes())
			{
				for (int d = 0; d < numDofsPerNode; d++)
				{
					IDofType dof = dofsPerNode[d];
					if (freeDofs.TryGetValue(node.ID, allDofs.GetIdOfDof(dof), out int freeDofIdx))
					{
						result[freeDofIdx] = numDofsPerNode * node.ID + d;
					}
				}
			}

			return result;
		}
	}
}
