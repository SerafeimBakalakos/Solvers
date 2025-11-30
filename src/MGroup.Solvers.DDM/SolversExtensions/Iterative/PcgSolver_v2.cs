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
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class PcgSolver_v2 : ISubstructureSystemSolver
	{
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly CsrMatrixAssembler_v2 matrixAssembler = new CsrMatrixAssembler_v2();

		private readonly IPreconditioner preconditioner;
		private bool mustUpdatePreconditioner = true;

		public PcgSolver_v2(ISubstructure substructure, PcgAlgorithm pcgAlgorithm, IPreconditioner preconditioner)
		{
			Substructure = substructure;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			var dofOrdering = new GlobalSubstructureDofOrdering(substructure, null);
			Problem = new GlobalSubstructureProblem(substructure, dofOrdering);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISolverLogger Logger { get; }

		public ISubstructureProblem Problem { get; }

		public ISubstructure Substructure { get; }

		public void PrepareDofs()
		{
			Problem.OrderDofs();
		}

		public void BuildSystemMatrix()
		{
			Problem.SystemMatrix = matrixAssembler.BuildSubstructureMatrix(Substructure, Problem.DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (Problem.SystemSolution == null)
			{
				Problem.SystemSolution = Problem.SystemRhs.CreateZeroVectorWithSameFormat();
			}
			else
			{
				Problem.SystemSolution.Clear();
			}

			// Preconditioning
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(Problem.SystemMatrix, !matrixPatternWillNotBeModified);
				mustUpdatePreconditioner = false;
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(Problem.SystemMatrix, preconditioner, Problem.SystemRhs, Problem.SystemSolution, true); //TODO: This way, we don't know that x0=0, which will result in an extra b-A*0
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
