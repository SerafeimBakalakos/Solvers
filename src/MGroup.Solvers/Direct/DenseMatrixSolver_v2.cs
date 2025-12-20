namespace MGroup.Solvers.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.LinearSystem;

	public class DenseMatrixSolver_v2 : ISolver_v2
	{
		private readonly bool isMatrixPositiveDefinite;
		private readonly DenseMatrixAssembler_v2 matrixAssembler = new DenseMatrixAssembler_v2();

		private Matrix inverse;

		//private readonly reordering = new NullReordering(); Why do I need NullReordering? Solvers that do not need to reorder can just not call the ISubdomain.ReorderDofs() method

		public DenseMatrixSolver_v2(ISubdomain_v2 domain, bool isMatrixPositiveDefinite)
		{
			this.Domain = domain;
			this.isMatrixPositiveDefinite = isMatrixPositiveDefinite;
			DofOrdering = new DefaultSubdomainDofOrdering_v2(domain, null);
			LinearSystem = new LinearSystem_v2();
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(DenseMatrixSolver_v2).Name);

		public ISubdomain_v2 Domain { get; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new MonolithicAlgebraicModel_v2(physicalModel, DofOrdering);
		}

		public void PrepareDofs()
		{
			DofOrdering.OrderDofs();
			DofOrdering.PrepareDofMaps();
			LinearSystem.RhsVector = Vector.CreateZero(DofOrdering.NumDofs);
		}

		public void BuildSystemMatrix()
		{
			LinearSystem.Matrix = matrixAssembler.BuildSubdomainMatrix(Domain, DofOrdering);
		}

		public void SolveLinearSystem()
		{
			var watch = new Stopwatch();
			if (LinearSystem.Solution == null)
			{
				LinearSystem.Solution = LinearSystem.RhsVector.CreateZeroVectorWithSameFormat();
			}
			else
			{
				LinearSystem.Solution.Clear();
			}

			// Factorization
			if (inverse == null)
			{
				watch.Start();
				var systemMatrix = (Matrix)LinearSystem.Matrix;
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
			inverse.MultiplyIntoResult(LinearSystem.RhsVector, LinearSystem.Solution);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
		}
	}
}
