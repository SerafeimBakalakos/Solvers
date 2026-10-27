namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algorithms;

	public class PcgSolverWithMultigridPreconditioner : ISolver
	{
		private readonly GlobalAlgebraicModel<CsrMatrix> algebraicModel;
		private readonly IMultigridAlgorithm multigrid;
		private readonly PcgAlgorithm pcg;
		private readonly MultigridPreconditioner preconditioner;

		private bool isInitialized = false;
		private bool systemMatrixHasChanged = true;

		public PcgSolverWithMultigridPreconditioner(GlobalAlgebraicModel<CsrMatrix> algebraicModel, IMultigridAlgorithm multigrid, PcgAlgorithm pcg)
		{
			this.algebraicModel = algebraicModel;
			this.multigrid = multigrid;
			this.pcg = pcg;
			this.preconditioner = new MultigridPreconditioner(multigrid);

			Name = "PCG solver with " + multigrid.Name + " preconditioner";
			Logger = multigrid.Logger;

			LinearSystem = algebraicModel.LinearSystem;
			LinearSystem.Observers.Add(this);
		}

		public IAlgebraicModel AlgebraicModel => algebraicModel;

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public ISolverLogger Logger { get; }

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; set; }

		public string Name { get; }

		public void HandleMatrixWillBeSet()
		{
			systemMatrixHasChanged = true;
		}

		public void Initialize()
		{
			throw new NotImplementedException("This method is not called by all analyzers. At least not by Linear or Static");
		}

		public void PreventFromOverwrittingSystemMatrices() { }

		public void Solve()
		{
			if (!isInitialized)
			{
				multigrid.Initialize();
				isInitialized = true;
			}

			if (systemMatrixHasChanged)
			{
				multigrid.PrepareForNewLinearSystemMatrix(LinearSystem.Matrix);
			}

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
	}
}
