namespace MGroup.Solvers.DDM.SolversExtensions.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.Logging;

	public class DenseMatrixSolver_v2 : ISubstructureSystemSolver
	{
		private readonly bool isMatrixPositiveDefinite;
		private readonly DenseMatrixAssembler_v2 matrixAssembler = new DenseMatrixAssembler_v2();

		private Matrix inverse;

		//private readonly reordering = new NullReordering(); Why do I need NullReordering? Solvers that do not need to reorder can just not call the ISubstructure.ReorderDofs() method

		public DenseMatrixSolver_v2(ISubstructure substructure, bool isMatrixPositiveDefinite)
		{
			this.Substructure = substructure;
			this.isMatrixPositiveDefinite = isMatrixPositiveDefinite;
			var dofOrdering = new GlobalSubstructureDofOrdering(substructure, null);
			Problem = new GlobalSubstructureProblem(substructure, dofOrdering);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(DenseMatrixSolver_v2).Name);

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

			// Factorization
			if (inverse == null)
			{
				watch.Start();
				var systemMatrix = (Matrix)(Problem.SystemMatrix);
				if (isMatrixPositiveDefinite)
				{
					inverse = systemMatrix.FactorCholesky(CanOverwriteSystemMatrices).Invert(true);
				}
				else
				{
					inverse = systemMatrix.FactorLU(CanOverwriteSystemMatrices).Invert(true);
				}

				watch.Stop();
				Logger.LogTaskDuration("Matrix factorization", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Substitutions
			watch.Start();
			inverse.MultiplyIntoResult(Problem.SystemRhs, Problem.SystemSolution);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
		}
	}
}
