namespace MGroup.Solvers.Multigrid.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.Algorithms;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.Geometric.Grids;
	using MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy;
	using MGroup.Solvers.Multigrid.Geometric.Prolongations;
	using MGroup.Solvers.Multigrid.Logging;
	using MGroup.Solvers.Multigrid.Restriction;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class GmgAlgorithm : MultigridAlgorithmBase
	{
		private const string name = "Geometric Multigrid";
		private readonly IGridHierarchy gridHierarchy;
		private readonly GmgOperatorHierarchyGalerkin gmgOperatorHierarchy;

		public GmgAlgorithm(IGridHierarchy gridHierarchy, GmgOperatorHierarchyGalerkin operatorHierarchy, MultigridSmoothersHierarchy smoothersHierarchy, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance)
			: base(operatorHierarchy, smoothersHierarchy, coarsestSystemSolver, cycleSchedule, maxCycles, residualTolerance, name)
		{
			this.gmgOperatorHierarchy = operatorHierarchy;
		}

		public override void Initialize()
		{
			Logger.ExecuteTimeAndLog("Define exact grids", () =>
			{
				gridHierarchy.DefineGrids();
			});

			Logger.ExecuteTimeAndLog("Prepare level progression", () =>
			{
				levelProgression = cycleSchedule.CreateProgression(gridHierarchy.NumLevels);
			});
		}

		protected override void PrepareLevelProgression() { } // Done at initialization and will not change.

		protected override void PrepareOperatorHierarchy(CsrMatrix systemMatrix)
		{
			Logger.ExecuteTimeAndLog("Prepare prolongations, restrictions and coarse matrices", () =>
			{
				gmgOperatorHierarchy.BuildOperators(systemMatrix);
			});

			Logger.LogOperatorHierarchy(gmgOperatorHierarchy.Log);
		}

		public class Builder : MultigridAlgorithmBuilderBase
		{
			private readonly IGeometricProlongationStrategy prolongationStrategy;
			private readonly IUniformGrid finestGrid;

			private IGridHierarchy gridHierarchy;

			public Builder(IGeometricProlongationStrategy prolongationStrategy, IUniformGrid finestGrid, int numLevelsRequested, int maxCycles) 
				: base(numLevelsRequested, maxCycles)
			{
				this.prolongationStrategy = prolongationStrategy;
				this.finestGrid = finestGrid;

				var coarseningRatios = new int[finestGrid.Dimension];
				Array.Fill(coarseningRatios, 2);
				gridHierarchy = new UniformGridHierarchy(numLevelsRequested, finestGrid, coarseningRatios);

				RestrictionStrategy = new FullWeightingRestrictionStrategy(finestGrid.Dimension);
			}

			public IGridHierarchy GridHierarchy 
			{ 
				set
				{
					if (value.Dimension != finestGrid.Dimension)
					{
						throw new ArgumentNullException("This grid hierarchy does not match the finest grid");
					}

					this.gridHierarchy = value;
				}
			}

			public GmgAlgorithm Build(IFreeDofProvider freeDofProvider)
			{
				var operatorHierarchy = new GmgOperatorHierarchyGalerkin(gridHierarchy, freeDofProvider, prolongationStrategy, RestrictionStrategy);
				CoarsestSystemSolver = FinalizeCoarsestSystemSolver();
				return new GmgAlgorithm(gridHierarchy, operatorHierarchy, smoothersHierarchy, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance);
			}
		}
	}
}
