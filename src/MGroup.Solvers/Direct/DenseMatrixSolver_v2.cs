namespace MGroup.Solvers.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;

	public class DenseMatrixSolver_v2 : ISolver_v2
	{
		private readonly bool isMatrixPositiveDefinite;
		private readonly DenseMatrixAssembler_v2 matrixAssembler = new DenseMatrixAssembler_v2();

		private Matrix inverse;

		public DenseMatrixSolver_v2(ISubdomain_v2 domain, IDofOrderingStrategy_v2 dofOrderingStrategy, bool isMatrixPositiveDefinite,  bool cacheElementDofs)
		{
			this.Domain = domain;
			this.isMatrixPositiveDefinite = isMatrixPositiveDefinite;
			LinearSystem = new LinearSystem_v2();
			DofManager = cacheElementDofs
				? new MonolithicDomainDofManagerCaching(domain, dofOrderingStrategy, null)
				: new MonolithicDomainDofManager(domain, dofOrderingStrategy, null);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public IMonolithicDofManager DofManager { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(DenseMatrixSolver_v2).Name);

		public ISubdomain_v2 Domain { get; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new MonolithicAlgebraicModel_v2(physicalModel, DofManager);
		}

		public void PrepareDofs()
		{
			DofManager.PrepareDofs();
			LinearSystem.RhsVector = Vector.CreateZero(DofManager.NumDomainDofs);
		}

		public void BuildSystemMatrix()
		{
			LinearSystem.Matrix = matrixAssembler.BuildSubdomainMatrix(Domain, DofManager);
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

		public class Factory
		{
			public Factory()
			{
			}

			public bool CacheElementDofs { get; set; } = true;

			public IDofOrderingStrategy_v2 DofOrderingStrategy { get; set; } = new DefaultDofOrdering(sortNodes: true, sortDofs: true);

			public bool IsMatrixPositiveDefinite { get; set; } = false;

			public DenseMatrixSolver_v2 CreateSolver(ISubdomain_v2 domain)
			{
				return new DenseMatrixSolver_v2(domain, DofOrderingStrategy, CacheElementDofs, IsMatrixPositiveDefinite);
			}
		}
	}
}
