namespace MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algorithms;
	using MGroup.Solvers.Multigrid.GalerkinProducts;
	using MGroup.Solvers.Multigrid.Geometric.Grids;
	using MGroup.Solvers.Multigrid.Geometric.Prolongations;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Restriction;

	public class GmgOperatorHierarchyGalerkin : IMultigridOperatorHierarchy
	{
		private readonly IGridHierarchy gridHierarchy;
		private readonly IGeometricProlongationStrategy prolongationStrategy;
		private readonly IRestrictionStrategy restrictionStrategy;
		private readonly GalerkinProductCsr galerkinProductStrategy = new();
		private readonly IFreeDofProvider freeDofProvider;

		// Necessary
		private readonly List<CsrMatrix> prolongationsPerLevel = new();
		private readonly List<CsrMatrix> restrictionsPerLevel = new();
		private readonly List<CsrMatrix> systemMatricesPerLevel = new();

		// Optional
		private readonly List<int[]> freeDofMapPerLevel = new();

		public GmgOperatorHierarchyGalerkin(IGridHierarchy gridHierarchy, IFreeDofProvider freeDofProvider, IGeometricProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy)
		{
			this.gridHierarchy = gridHierarchy;
			this.prolongationStrategy = prolongationStrategy;
			this.restrictionStrategy = restrictionStrategy;
			this.freeDofProvider = freeDofProvider;
		}

		public MultigridOperatorHierarchyLog Log { get; private set; }

		public int NumLevels => gridHierarchy.NumLevels;

		public void BuildOperators(CsrMatrix finestLevelSystemMatrix)
		{
			Clear();

			systemMatricesPerLevel.Add(finestLevelSystemMatrix);
			Log.LogSystemMatrix(finestLevelSystemMatrix);

			freeDofMapPerLevel.Add(freeDofProvider.FindFreeDofs());

			for (int lvl = 0; lvl < gridHierarchy.NumLevels - 1; lvl++)
			{
				// Prolongation matrix without taking into account the constrained dofs
				IUniformGrid fineGrid = gridHierarchy.GetGridAtLevel(lvl);
				IUniformGrid coarseGrid = gridHierarchy.GetGridAtLevel(lvl + 1);
				DokRowMajor prolongationReference = prolongationStrategy.CreateProlongationMatrix(fineGrid, coarseGrid);

				// Keep only the free dofs for the fine and coarse grid
				int[] freeDofsFine = freeDofMapPerLevel[lvl];
				int[] freeDofsCoarse = FindFreeDofsCoarse(prolongationReference, freeDofsFine);
				freeDofMapPerLevel.Add(freeDofsCoarse);

				DokRowMajor prolongationFreeDok = prolongationReference.GetSubmatrix(freeDofsFine, freeDofsCoarse);
				CsrMatrix P = prolongationFreeDok.BuildCsrMatrix(true);
				prolongationsPerLevel.Add(P);
				Log.LogProlongation(P);

				// Restriction matrix
				CsrMatrix R = restrictionStrategy.CalcRestrictionMatrix(prolongationsPerLevel[lvl]);
				restrictionsPerLevel.Add(R);
				Log.LogRestriction(R);

				// System matrix (with Galerkin product)
				CsrMatrix Af = systemMatricesPerLevel[lvl];
				CsrMatrix Ac = galerkinProductStrategy.CalcProduct(R, Af, P);
				systemMatricesPerLevel.Add(Ac);
				Log.LogSystemMatrix(Ac);
			}
		}

		public void Clear()
		{
			Log = new MultigridOperatorHierarchyLog();
			prolongationsPerLevel.Clear();
			restrictionsPerLevel.Clear();
			systemMatricesPerLevel.Clear();
			freeDofMapPerLevel.Clear();
		}

		public CsrMatrix GetProlongation(int level)
		{
			CheckFineLevel(level);
			return prolongationsPerLevel[level];
		}

		public CsrMatrix GetRestriction(int level)
		{
			CheckFineLevel(level);
			return restrictionsPerLevel[level];
		}

		public CsrMatrix GetSystemMatrix(int level) => systemMatricesPerLevel[level];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckFineLevel(int level)
		{
			if (level >= NumLevels - 1) throw new ArgumentException("This data is stored only for levels corresponding to fine-grids");
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
	}
}
