namespace MGroup.Solvers.Direct
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.LinearSystem;

	public class CholeskyCscSolver_v2 : ISolver_v2, IDisposable
	{
		private readonly IImplementationProvider laImplementation;
		private readonly SymmetricCscMatrixAssembler_v2 matrixAssembler = new SymmetricCscMatrixAssembler_v2();

		private ICholeskySymmetricCsc factorization;

		public CholeskyCscSolver_v2(ISubdomain_v2 domain, IImplementationProvider laImplementation, IReorderingAlgorithm reorderingAlgorithm = null, bool cacheElementDofs = true)
		{
			this.Domain = domain;
			this.laImplementation = laImplementation;
			if (reorderingAlgorithm == null)
			{
				reorderingAlgorithm = new AmdSymmetricOrdering(laImplementation);
			}

			DofOrdering = new MonolithicDomainDofOrdering(domain, reorderingAlgorithm);
			LinearSystem = new LinearSystem_v2();
		}

		~CholeskyCscSolver_v2()
		{
			ReleaseResources();
		}

		public void Dispose()
		{
			ReleaseResources();
			GC.SuppressFinalize(this);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(nameof(CholeskyCscSolver_v2));

		public ISubdomain_v2 Domain { get; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new MonolithicAlgebraicModel_v2(physicalModel, DofOrdering);
		}

		public void PrepareDofs()
		{
			DofOrdering.OrderDofs();
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

			var systemMatrix = (SymmetricCscMatrix)(LinearSystem.Matrix);
			var systemRhs = (Vector)(LinearSystem.RhsVector);
			var systemSolution = (Vector)(LinearSystem.Solution);

			// Factorization
			if (factorization == null)
			{
				watch.Start();
				factorization = laImplementation.CreateCholeskyTriangulation();
				factorization.Factorize(systemMatrix);
				watch.Stop();
				Logger.LogTaskDuration("Matrix factorization", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Substitutions
			watch.Start();
			factorization.SolveLinearSystem(systemRhs, systemSolution);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
		}

		private void ReleaseResources()
		{
			if (factorization != null)
			{
				factorization.Dispose();
				factorization = null;
			}
		}
	}
}
