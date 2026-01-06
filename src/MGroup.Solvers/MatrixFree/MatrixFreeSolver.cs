namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reduction;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Iterative;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.Preconditioning;

	public class MatrixFreeSolver : ISolver_v2
	{
		private readonly IDofScaling dofScaling;
		private readonly IComputeEnvironment environment;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly IElementPartition partition;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IMatrixFreePreconditioner preconditioner;

		private bool mustUpdatePreconditioner = true;
		private DistributedOverlappingIndexer dofIndexer;

		public MatrixFreeSolver(IComputeEnvironment environment, ISubdomain_v2 domain, IElementPartition partition, PcgAlgorithm pcgAlgorithm, IMatrixFreePreconditioner preconditioner, bool isHomogeneous)
		{
			this.environment = environment;
			Domain = domain;
			this.partition = partition;
			this.pcgAlgorithm = pcgAlgorithm;
			this.preconditioner = preconditioner;
			DofOrdering = new DistributedDofOrdering(environment, domain, partition);
			LinearSystem = new LinearSystem_v2();

			if (isHomogeneous)
			{
				this.dofScaling = new HomogeneousDofScaling(environment, Domain, partition, DofOrdering);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public DistributedDofOrdering DofOrdering { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(PcgSolver_v2).Name);

		public ISubdomain_v2 Domain { get; }

		public IterativeStatistics IterativeAlgorithmStats { get; private set; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new MatrixFreeAlgebraicModel(environment, physicalModel, Domain, LinearSystem, DofOrdering);
		}

		public void PrepareDofs()
		{
			// Indexer for distributed vectors and matrices
			partition.FindElementNeighbors();
			dofIndexer = DofOrdering.CreateIndexer();

			LinearSystem.RhsVector = new DistributedOverlappingVector(dofIndexer);
		}

		public void BuildSystemMatrix()
		{
			var distributedMatrix = new DistributedOverlappingMatrix<IMatrix>(dofIndexer);
			environment.DoPerNode(elementID =>
			{
				ISuperElement element = Domain.GetElement(elementID);
				distributedMatrix.LocalMatrices[elementID] = element.BuildMatrix();
			});
			LinearSystem.Matrix = distributedMatrix;
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
				preconditioner.Update(LinearSystem.Matrix, null, null, dofScaling);
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
