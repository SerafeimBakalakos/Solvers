namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using CSparse;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.ConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.CoarseMatrix;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class PcgWithMultigridSolver : MultigridSolverBase
	{
		private readonly PcgAlgorithm pcg;
		private readonly MultigridPreconditioner preconditioner;

		internal PcgWithMultigridSolver(GlobalAlgebraicModel<CsrMatrix> algebraicModel, Model model, IDofType[] dofsPerNode, int numLevels, IGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, bool useGalerkinCoarseMatrices, PcgAlgorithm pcg)
			: base(algebraicModel, model, dofsPerNode, numLevels, finestGrid, coarseningRatios, prolongationStrategy, restrictionStrategy, smoothers, coarsestSystemSolver, cycleSchedule, maxCycles, residualTolerance, useGalerkinCoarseMatrices, "PcgWithMultigridPreconditionerSolver")
		{
			this.pcg = pcg;
			this.preconditioner = new MultigridPreconditioner(ApplyPreconditioner);
		}

		public override void Solve()
		{
			PrepareForNewLinearSystem();

			var watch = new Stopwatch();
			watch.Start();

			IReadOnlyMatrix finestMatrix = LinearSystem.Matrix;
			IterativeStatistics stats = pcg.Solve(finestMatrix, preconditioner, LinearSystem.RhsVector, LinearSystem.Solution, true);
			
			watch.Stop();
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Logger.LogTaskDuration("Execution of cycles", watch.ElapsedMilliseconds);

			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm run for"
					+ $" {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}
		}

		private void ApplyPreconditioner(IReadOnlyVector rhs, IVector lhs)
		{
			lhs.Clear();
			vectorsRhs[0] = (Vector)rhs;
			vectorsLhs[0] = (Vector)lhs;
			RunSingleCycle();
		}

		public class Builder : MultigridSolverBuilderBase
		{
			public Builder(int numLevels, IGrid finestGrid, IProlongationStrategy prolongation, int maxCycles)
				: base(numLevels, finestGrid, prolongation, maxCycles)
			{
			}

			public PcgAlgorithm.Factory PcgBuilder { get; set; } = new PcgAlgorithm.Factory();

			public PcgWithMultigridSolver BuildSolver(Model model, IDofType[] dofsPerNode, GlobalAlgebraicModel<CsrMatrix> algebraicModel)
			{
				PcgBuilder.ResidualTolerance = this.ResidualTolerance;
				PcgBuilder.MaxIterationsProvider = new FixedMaxIterationsProvider(maxCycles);
				PcgAlgorithm pcg = PcgBuilder.Build();

				CoarseningRatioPerAxis = FinalizeCoarseningRatios();
				CoarsestSystemSolver = FinalizeCoarsetSystemSolver();

				return new PcgWithMultigridSolver(algebraicModel, model, dofsPerNode, numLevels, finestGrid, CoarseningRatioPerAxis, prolongation, Restriction, smoothers, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance, useGalerkinCoarseMatrices, pcg);
			}
		}
	}
}
