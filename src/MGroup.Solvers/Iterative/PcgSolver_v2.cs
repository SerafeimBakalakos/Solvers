namespace MGroup.Solvers.Iterative
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
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;

	public class PcgSolver_v2 : ISolver_v2
	{
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly CsrMatrixAssembler_v2 matrixAssembler = new CsrMatrixAssembler_v2();
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IPreconditioner preconditioner;

		private bool mustUpdatePreconditioner = true;

		public PcgSolver_v2(ISubdomain_v2 domain, PcgAlgorithm pcgAlgorithm, IPreconditioner preconditioner, IDofOrderingStrategy_v2 dofOrderingStrategy,  bool cacheElementDofs = true)
		{
			Domain = domain;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			LinearSystem = new LinearSystem_v2();
			DofManager = cacheElementDofs
				? new MonolithicDomainDofManagerCaching(domain, dofOrderingStrategy, null)
				: new MonolithicDomainDofManager(domain, dofOrderingStrategy, null);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public IMonolithicDofManager DofManager { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(PcgSolver_v2).Name);

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

			// Preconditioning
			if (mustUpdatePreconditioner)
			{
				watch.Start();
				preconditioner.UpdateMatrix(LinearSystem.Matrix, !matrixPatternWillNotBeModified);
				mustUpdatePreconditioner = false;
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(LinearSystem.Matrix, preconditioner, LinearSystem.RhsVector, LinearSystem.Solution, true);
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

		public class Factory
		{
			public Factory()
			{
			}

			public bool CacheElementDofs { get; set; } = true;

			public IDofOrderingStrategy_v2 DofOrderingStrategy { get; set; } = new DefaultDofOrdering(sortNodes: true, sortDofs: true);

			public PcgAlgorithm PcgAlgorithm { get; set; } = (new PcgAlgorithm.Factory()).Build();

			public IPreconditioner Preconditioner { get; set; } = new JacobiPreconditioner();

			public PcgSolver_v2 CreateSolver(ISubdomain_v2 domain)
			{
				return new PcgSolver_v2(domain, PcgAlgorithm, Preconditioner, DofOrderingStrategy, CacheElementDofs);
			}
		}
	}
}
