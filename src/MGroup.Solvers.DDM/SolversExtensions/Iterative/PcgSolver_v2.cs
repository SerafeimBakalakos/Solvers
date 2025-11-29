namespace MGroup.Solvers.DDM.SolversExtensions.Iterative
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class PcgSolver_v2 : ISubstructureSystemSolver
	{
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly CsrMatrixAssembler_v2 matrixAssembler = new CsrMatrixAssembler_v2();
		private readonly DenseVectorAssembler vectorAssembler = new DenseVectorAssembler();

		private readonly IPreconditioner preconditioner;
		private bool mustUpdatePreconditioner = true;

		public PcgSolver_v2(PcgAlgorithm pcgAlgorithm, IPreconditioner preconditioner)
		{
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubstructureDofOrdering DofOrdering { get; set; }

		public ISolverLogger Logger { get; }

		public IMatrix Matrix { get; set; }

		public IVector Rhs { get; set; }

		public IVector Solution { get; set; }

		public ISubstructure Substructure { get; set; }

		public void PrepareDofs()
		{
			DofOrdering.PrepareDofMaps();
		}

		public void PrepareLinearSystem()
		{
			var watch = new Stopwatch();
			Matrix = matrixAssembler.BuildSubstructureMatrix(Substructure, DofOrdering);
			Rhs = vectorAssembler.BuildSubstructureVector(Substructure, DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (Solution == null)
			{
				Solution = Rhs.CreateZeroVectorWithSameFormat();
			}
			else
			{
				Solution.Clear();
			}

			// Preconditioning
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(Matrix, !matrixPatternWillNotBeModified);
				mustUpdatePreconditioner = false;
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(Matrix, preconditioner, Rhs, Solution, true); //TODO: This way, we don't know that x0=0, which will result in an extra b-A*0
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(typeof(PcgSolver_v2).Name
					+ $" did not converge to a solution. PCG algorithm run for {stats.NumIterationsRequired} iterations and the residual norm ratio was {stats.ResidualNormRatioEstimation}");
			}

			watch.Stop();
			Logger.LogTaskDuration("Iterative algorithm", watch.ElapsedMilliseconds);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Logger.IncrementAnalysisStep();
		}
	}
}
