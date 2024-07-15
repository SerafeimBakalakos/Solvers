namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using MGroup.LinearAlgebra.AlgebraicMultiGrid.PodAmg;
	using MGroup.LinearAlgebra.AlgebraicMultiGrid;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Iterative.Stationary.CSR;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.AlgebraicModel;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.Solvers.Direct;
	using Serilog.Core;
	using System.Diagnostics;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.MSolve.Solution.AlgebraicModel;

	public class TempSolverIterative : SingleSubdomainSolverBase<CsrMatrix>, ITempSolver
	{
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly bool matrixPatternWillNotBeModified;
		private readonly IPreconditioner preconditioner;

		private bool mustUpdatePreconditioner = true;

		private int currentParameterSet;
		private int currentTimeStep;

		private TempSolverIterative(GlobalAlgebraicModel<CsrMatrix> model, PcgAlgorithm pcgAlgorithm,
			IPreconditioner preconditioner, bool matrixPatternWillNotBeModified)
			: base(model, "PcgSolver")
		{
			this.pcgAlgorithm = pcgAlgorithm;
			this.matrixPatternWillNotBeModified = matrixPatternWillNotBeModified;
			this.preconditioner = preconditioner;
		}

		public IAlgebraicModel Model => model;

		public SolutionDatabaseOLD SavedSolutions { get; } = new SolutionDatabaseOLD();

		public override void HandleMatrixWillBeSet()
		{
			mustUpdatePreconditioner = true;
		}

		public override void Initialize() { }

		public void OnModelParameterUpdate(int parameterSet)
		{
			currentParameterSet = parameterSet;
			currentTimeStep = 0;
		}

		public override void PreventFromOverwrittingSystemMatrices()
		{
			// No factorization is done.
		}

		/// <summary>
		/// Solves the linear system with PCG method. If the matrix has been modified, a new preconditioner will be computed.
		/// </summary>
		public override void Solve()
		{
			var watch = new Stopwatch();

			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;
			if (LinearSystem.Solution.SingleVector == null)
			{
				LinearSystem.Solution.SingleVector = Vector.CreateZero(systemSize);
			}
			else LinearSystem.Solution.Clear();

			// Preconditioning
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
				mustUpdatePreconditioner = false;
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(matrix, preconditioner,
				LinearSystem.RhsVector.SingleVector, LinearSystem.Solution.SingleVector,
				true, () => Vector.CreateZero(systemSize)); //TODO: This way, we don't know that x0=0, which will result in an extra b-A*0
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm run for"
					+ $" {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}
			watch.Stop();
			Logger.LogTaskDuration("Iterative algorithm", watch.ElapsedMilliseconds);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Logger.IncrementAnalysisStep();

			SavedSolutions.SaveSolution(currentParameterSet, currentTimeStep, LinearSystem.Solution);
			++currentTimeStep;
		}

		protected override Matrix InverseSystemMatrixTimesOtherMatrix(IMatrixView otherMatrix)
		{
			//TODO: Use a reorthogonalizetion approach when solving multiple rhs vectors. It would be even better if the CG
			//      algorithm exposed a method for solving for multiple rhs vectors.
			var watch = new Stopwatch();

			// Preconditioning
			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
				mustUpdatePreconditioner = false;
			}

			// Iterative algorithm
			watch.Start();
			int numRhs = otherMatrix.NumColumns;
			var solutionVectors = Matrix.CreateZero(systemSize, numRhs);
			var solutionVector = Vector.CreateZero(systemSize);

			// Solve each linear system
			for (int j = 0; j < numRhs; ++j)
			{
				if (j != 0) solutionVector.Clear();

				//TODO: we should make sure this is the same type as the vectors used by this solver, otherwise vector operations
				//      in CG will be slow.
				Vector rhsVector = otherMatrix.GetColumn(j);

				IterativeStatistics stats = pcgAlgorithm.Solve(matrix, preconditioner, rhsVector,
					solutionVector, true, () => Vector.CreateZero(systemSize));

				solutionVectors.SetSubcolumn(j, solutionVector);
			}

			watch.Stop();
			Logger.LogTaskDuration("Iterative algorithm", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
			return solutionVectors;
		}

		public class Factory
		{
			public IDofOrderer DofOrderer { get; set; }
				= new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

			public bool MatrixPatternWillNotBeModified { get; set; } = false;

			public PcgAlgorithm PcgAlgorithm { get; set; } = (new PcgAlgorithm.Factory()).Build();

			public IPreconditioner Preconditioner { get; set; } = new JacobiPreconditioner();

			public TempSolverIterative BuildSolver(GlobalAlgebraicModel<CsrMatrix> model)
				=> new TempSolverIterative(model, PcgAlgorithm, Preconditioner.CopyWithInitialSettings(), MatrixPatternWillNotBeModified);

			public GlobalAlgebraicModel<CsrMatrix> BuildAlgebraicModel(IModel model)
				=> new GlobalAlgebraicModel<CsrMatrix>(model, DofOrderer, new CsrMatrixAssembler(true));
		}
	}
}
