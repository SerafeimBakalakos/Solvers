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
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
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
		private readonly IMatrixFreePreconditioner preconditioner;

		private bool mustUpdatePreconditioner = true;

		public MatrixFreeSolverMonolithic(ISubdomain_v2 domain, IElementPartition partition, PcgAlgorithm pcgAlgorithm, IMatrixFreePreconditioner preconditioner, bool isHomogeneous)
		{
			Domain = domain;
			elements = Domain.EnumerateElements().ToList();
			this.partition = partition;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			DofOrdering = new MonolithicDomainDofOrdering_v2(domain, null);
			LinearSystem = new LinearSystem_v2();

			if (isHomogeneous)
			{
				dofScaling = new HomogeneousDofScalingMonolithic(partition, elements, DofOrdering);
			}
			else
			{
				throw new NotImplementedException();
			}	
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public ISubdomainDofOrdering_v2 DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(PcgSolver_v2).Name);

		public ISubdomain_v2 Domain { get; }

		public IterativeStatistics IterativeAlgorithmStats { get; private set; }

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
			LinearSystem.Matrix = new ElementWiseMatrixMonolithic(elements, DofOrdering);
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
				preconditioner.Update(LinearSystem.Matrix, elements, DofOrdering, dofScaling);
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
