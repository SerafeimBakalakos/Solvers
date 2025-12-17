namespace MGroup.Solvers.DDM.SolversExtensions.Direct
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
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.Assemblers;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.Logging;

	public class DenseMatrixSolver_v2 : ISubdomainSystemSolver
	{
		private readonly bool isMatrixPositiveDefinite;
		private readonly DenseMatrixAssembler_v2 matrixAssembler = new DenseMatrixAssembler_v2();

		private Matrix inverse;

		//private readonly reordering = new NullReordering(); Why do I need NullReordering? Solvers that do not need to reorder can just not call the ISubdomain.ReorderDofs() method

		public DenseMatrixSolver_v2(ISubdomain_v2 subdomain, bool isMatrixPositiveDefinite)
		{
			this.Subdomain = subdomain;
			this.isMatrixPositiveDefinite = isMatrixPositiveDefinite;
			DofOrdering = new DefaultSubdomainDofOrdering(subdomain, null);
			LinearSystem = new LinearSystem_v2();
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(DenseMatrixSolver_v2).Name);

		public ISubdomain_v2 Subdomain { get; }

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
			LinearSystem.Matrix = matrixAssembler.BuildSubdomainMatrix(Subdomain, DofOrdering);
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
