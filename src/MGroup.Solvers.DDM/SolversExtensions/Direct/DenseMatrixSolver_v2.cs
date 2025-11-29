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
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class DenseMatrixSolver_v2 : ISubstructureSystemSolver
	{
		private readonly bool isMatrixPositiveDefinite;
		private readonly DenseMatrixAssembler_v2 matrixAssembler = new DenseMatrixAssembler_v2();
		private readonly DenseVectorAssembler vectorAssembler = new DenseVectorAssembler();

		private Matrix inverse;

		//private readonly reordering = new NullReordering(); Why do I need NullReordering? Solvers that do not need to reorder can just not call the ISubstructure.ReorderDofs() method

		public DenseMatrixSolver_v2(ISubstructure substructure, bool isMatrixPositiveDefinite)
		{
			this.Substructure = substructure;
			this.isMatrixPositiveDefinite = isMatrixPositiveDefinite;
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

			// Factorization
			if (inverse == null)
			{
				watch.Start();
				var systemMatrix = (Matrix)Matrix;
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
			inverse.MultiplyIntoResult(Rhs, Solution);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
		}
	}
}
