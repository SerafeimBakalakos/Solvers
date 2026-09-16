namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra;
	using MGroup.LinearAlgebra.Implementations;
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
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.Multigrid.CoarseMatrix;
	using MGroup.Solvers.Multigrid.CoarseSystemSolvers;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;

	public class GeometricMultigridSolver : MultigridSolverBase
	{
		internal GeometricMultigridSolver(GlobalAlgebraicModel<CsrMatrix> algebraicModel, Model model, IDofType[] dofsPerNode, int numLevels, IGrid finestGrid, int[] coarseningRatios, IProlongationStrategy prolongationStrategy, IRestrictionStrategy restrictionStrategy, MultigridSmoothers smoothers, ICoarseSystemSolver coarsestSystemSolver, ICycleSchedule cycleSchedule, int maxCycles, double residualTolerance, bool useGalerkinCoarseMatrices)
			: base(algebraicModel, model, dofsPerNode, numLevels, finestGrid, coarseningRatios, prolongationStrategy, restrictionStrategy, smoothers, coarsestSystemSolver, cycleSchedule, maxCycles, residualTolerance, useGalerkinCoarseMatrices, "GeometricMultigridSolver")
		{
		}

		public override void Solve()
		{
			PrepareForNewLinearSystem();

			var watch = new Stopwatch();
			watch.Start();

			vectorsRhs[0] = LinearSystem.RhsVector;
			vectorsLhs[0] = LinearSystem.Solution;

			Vector b = LinearSystem.RhsVector;
			var res = Vector.CreateZero(b.Length);
			double normRes0 = b.Norm2();
			double resRatio = 1.0;
			int cycleIdx;
			for (cycleIdx = 0; cycleIdx < maxCycles; cycleIdx++)
			{
				RunSingleCycle();

				// Calculate the residual of the coarsest grid.
				//TODO: Perhaps this is already done inside the RunSingleCycle().
				Vector x = algebraicModel.LinearSystem.Solution;
				IReadOnlyMatrix matrix = algebraicModel.LinearSystem.Matrix;
				matrix.MultiplyIntoResult(x, res);
				res.LinearCombinationIntoThis(-1, b, 1);
				double normRes = res.Norm2();
				resRatio = normRes / normRes0;
				
				if (resRatio <= residualTolerance)
				{
					break;
				}
			}

			watch.Stop();
			Logger.LogIterativeAlgorithm(cycleIdx + 1, resRatio);
			Logger.LogTaskDuration("Execution of cycles", watch.ElapsedMilliseconds);

			if (resRatio > residualTolerance)
			{
				throw new IterativeSolverNotConvergedException(
					$"{Name} did not converge to a solution. The solver ran for {maxCycles} cycles iterations and ||b-A*x|| / ||b|| is {resRatio}.");
			}
		}

		public class Builder : MultigridSolverBuilderBase
		{
			public Builder(int numLevels, IGrid finestGrid, IProlongationStrategy prolongation, int maxCycles)
				: base(numLevels, finestGrid, prolongation, maxCycles)
			{
			}

			public GeometricMultigridSolver BuildSolver(Model model, IDofType[] dofsPerNode, GlobalAlgebraicModel<CsrMatrix> algebraicModel)
			{
				CoarseningRatioPerAxis = FinalizeCoarseningRatios();
				CoarsestSystemSolver = FinalizeCoarsetSystemSolver();

				return new GeometricMultigridSolver(algebraicModel, model, dofsPerNode, numLevels, finestGrid, CoarseningRatioPerAxis, prolongation, Restriction, smoothers, CoarsestSystemSolver, CycleSchedule, maxCycles, ResidualTolerance, useGalerkinCoarseMatrices);
			}
		}
	}
}
