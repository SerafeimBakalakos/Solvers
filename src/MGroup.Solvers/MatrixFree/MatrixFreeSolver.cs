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
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reduction;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.Iterative;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.ElementMatrices;
	using MGroup.Solvers.MatrixFree.Preconditioning;

	public class MatrixFreeSolver : ISolver_v2
	{
		private readonly IComputeEnvironment environment;
		private readonly bool matrixPatternWillNotBeModified = false;
		private readonly IElementPartition partition;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IPreconditioner preconditioner;
		private readonly IElementMatrixConverter elementMatrixConverter;
		private bool mustUpdatePreconditioner = true;

		public MatrixFreeSolver(IComputeEnvironment environment, IDomain domain, IElementPartition partition, PcgAlgorithm iterativeAlgorithm, IMatrixFreePreconditionerFactory preconditionerFactory, IElementMatrixConverter elementMatrixConverter, bool isHomogeneous)
		{
			this.environment = environment;
			Domain = domain;
			this.partition = partition;
			this.pcgAlgorithm = iterativeAlgorithm;
			this.elementMatrixConverter = elementMatrixConverter;
			DofManager = new DistributedDofManager(environment, domain, partition);
			LinearSystem = new LinearSystem_v2();

			IDofScaling dofScaling;
			if (isHomogeneous)
			{
				dofScaling = new HomogeneousDofScaling(environment, Domain, partition, DofManager);
			}
			else
			{
				dofScaling = new HeterogeneousDofScaling(environment, Domain, LinearSystem);
			}

			this.preconditioner = preconditionerFactory.CreatePreconditioner(dofScaling);
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public IDistributedDofManager DofManager { get; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; } = new SolverLogger(typeof(PcgSolver_v2).Name);

		public IDomain Domain { get; }

		public IterativeStatistics IterativeAlgorithmStats { get; private set; }

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new MatrixFreeAlgebraicModel(environment, physicalModel, Domain, LinearSystem, DofManager);
		}

		public void PrepareDofs()
		{
			// Indexer for distributed vectors and matrices
			partition.FindElementNeighbors();
			DofManager.PrepareDofs();

			LinearSystem.RhsVector = new DistributedOverlappingVector(DofManager.DistributedIndexer);
		}

		public void BuildSystemMatrix()
		{
			var distributedMatrix = new DistributedOverlappingMatrix<IMatrix>(DofManager.DistributedIndexer);
			environment.DoPerNode(elementID =>
			{
				ISuperElement element = Domain.GetElement(elementID);
				IMatrix elementMatrix = elementMatrixConverter.ConvertElementMatrix(element.BuildMatrix());
				distributedMatrix.LocalMatrices[elementID] = elementMatrix;
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
				preconditioner.UpdateMatrix(LinearSystem.Matrix, true);
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

		public class Factory
		{
			private readonly IComputeEnvironment environment;

			public Factory(IComputeEnvironment environment)
			{
				this.environment = environment;

				var pcgAlgorithmFactory = new PcgAlgorithm.Factory();
				IterativeAlgorithm = pcgAlgorithmFactory.Build();
			}

			public IElementMatrixConverter ElementMatrixConverter { get; set; } = new NullElementMatrixConverter();

			public bool IsMaterialHomogeneous { get; set; }

			public PcgAlgorithm IterativeAlgorithm { get; set; }

			public IMatrixFreePreconditionerFactory PreconditionerFactory { get; set; }
				= new MatrixFreeJacobiPreconditioner.Factory();

			public MatrixFreeSolver CreateSolver(IDomain domain, IElementPartition partition)
			{
				return new MatrixFreeSolver(environment, domain, partition, IterativeAlgorithm, PreconditionerFactory, ElementMatrixConverter, IsMaterialHomogeneous);
			}
		}
	}
}
