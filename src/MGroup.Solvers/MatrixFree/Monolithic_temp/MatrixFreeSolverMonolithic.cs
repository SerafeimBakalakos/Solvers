namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.Iterative;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.Preconditioning;

	public class MatrixFreeSolverMonolithic : ISolver_v2
	{
		private readonly IDofScaling dofScaling;
		private readonly IReadOnlyList<ISuperElement> elements;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly IElementPartition partition;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IMatrixFreePreconditionerMonolithic preconditioner;

		private bool mustUpdatePreconditioner = true;

		public MatrixFreeSolverMonolithic(IDomain domain, IElementPartition partition, PcgAlgorithm pcgAlgorithm, IMatrixFreePreconditionerMonolithic preconditioner, bool isHomogeneous, bool cacheElementDofs = true)
		{
			Domain = domain;
			elements = Domain.EnumerateElements().ToList();
			this.partition = partition;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			LinearSystem = new LinearSystem_v2();

			var dofOrderingStrategy = new DefaultDofOrdering(sortNodes: true, sortDofs: true);
			DofManager = cacheElementDofs
				? new MonolithicDomainDofManagerCaching(domain, dofOrderingStrategy, null)
				: new MonolithicDomainDofManager(domain, dofOrderingStrategy, null);

			if (isHomogeneous)
			{
				dofScaling = new HomogeneousDofScalingMonolithic(partition, elements);
			}
			else
			{
				throw new NotImplementedException();
			}	
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public IMonolithicDofManager DofManager { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(PcgSolver_v2).Name);

		public IDomain Domain { get; }

		public IterativeStatistics IterativeAlgorithmStats { get; private set; }

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
			LinearSystem.Matrix = new ElementWiseMatrixMonolithic(elements, DofManager);
		}

		public void SolveLinearSystem() //TODO: This is identical to PcgSolver
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
				//preconditioner.UpdateMatrix(LinearSystem.Matrix, !matrixPatternWillNotBeModified);
				preconditioner.Update(LinearSystem.Matrix, elements, DofManager, dofScaling);
				mustUpdatePreconditioner = false;
				watch.Stop();
				Logger.LogTaskDuration("Calculating preconditioner", watch.ElapsedMilliseconds);
				watch.Reset();
			}

			// Iterative algorithm
			watch.Start();
			IterativeStatistics stats = pcgAlgorithm.Solve(LinearSystem.Matrix, preconditioner, LinearSystem.RhsVector, LinearSystem.Solution, true);
			IterativeAlgorithmStats = stats;
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
