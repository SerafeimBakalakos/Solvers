namespace MGroup.Solvers.DDM.Psm
{
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reordering;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.Output;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.DDM.PSM.Dofs;
	using MGroup.Solvers.DDM.PSM.InterfaceProblem;
	using MGroup.Solvers.DDM.PSM.Preconditioning;
	using MGroup.Solvers.DDM.PSM.Reanalysis;
	using MGroup.Solvers.DDM.PSM.Scaling;
	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
	using MGroup.Solvers.DDM.PSM.Vectors;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;

	using TriangleNet.Topology;

	public class PsmSolver_v2<TMatrix> : ISolver_v2
		where TMatrix : class, IMatrix
	{
		private const bool cacheDistributedVectorBuffers = true;

		protected readonly bool directSolverIsParallel = false;
		protected readonly IComputeEnvironment environment;
		protected readonly IInitialSolutionGuessStrategy initialSolutionGuessStrategy;
		protected readonly IPsmInterfaceProblemMatrix interfaceProblemMatrix;
		protected readonly ISystemSolutionIterativeMethod interfaceProblemSolver;
		protected readonly IPsmInterfaceProblemVectors interfaceProblemVectors;
		protected readonly string name;
		//private readonly ObjectiveConvergenceCriterion<TMatrix> objectiveConvergenceCriterion;
		private readonly IPartition_v2 partition;
		protected /*readonly*/ IPsmPreconditioner preconditioner; //TODO: Make this readonly as well.
		protected readonly IImplementationProvider provider;
		//protected readonly PsmReanalysisOptions reanalysis;
		protected readonly IBoundaryDofScaling scaling;
		protected readonly ConcurrentDictionary<int, IMonolithicDofManager> subdomainDofsAll;
		protected readonly ConcurrentDictionary<int, PsmSubdomainDofs_v2> subdomainDofsPsm;
		protected readonly ConcurrentDictionary<int, ISubdomainLinearSystem_v2> subdomainLinearSystems;
		protected readonly ConcurrentDictionary<int, IPsmSubdomainMatrixManager_v2> subdomainMatricesPsm;
		protected readonly ConcurrentDictionary<int, IDomainMatrixAssembler_v2<TMatrix>> subdomainMatrixAssemblers;
		protected readonly ISubdomainTopology_v2 subdomainTopology;
		protected readonly ConcurrentDictionary<int, PsmSubdomainVectors_v2> subdomainVectorsPsm;

		protected int analysisIteration;
		protected DistributedOverlappingIndexer allDofIndexer;
		protected DistributedOverlappingIndexer boundaryDofIndexer;

		protected PsmSolver_v2(IComputeEnvironment environment, IDomain domain, IPartition_v2 partition,
			IImplementationProvider provider, IPsmSubdomainMatrixManagerFactory_v2<TMatrix> matrixManagerFactory,
			bool explicitSubdomainMatrices, IPsmPreconditioner preconditioner,
			IPsmInterfaceProblemSolverFactory interfaceProblemSolverFactory, bool isHomogeneous, DdmLogger logger,
			bool optimizedSubdomainTopology, /*PsmReanalysisOptions reanalysis,*/ IDofOrderingStrategy_v2 dofOrderingStrategy, bool cacheElementDofs)
		{
			this.environment = environment;
			Domain = domain;
			this.partition = partition;
			this.provider = provider;
			this.LinearSystem = new LinearSystem_v2();
			this.preconditioner = preconditioner;
			//this.reanalysis = reanalysis;

			this.subdomainDofsAll = new ConcurrentDictionary<int, IMonolithicDofManager>();
			this.subdomainDofsPsm = new ConcurrentDictionary<int, PsmSubdomainDofs_v2>();
			this.subdomainLinearSystems = new ConcurrentDictionary<int, ISubdomainLinearSystem_v2>();
			this.subdomainMatricesPsm = new ConcurrentDictionary<int, IPsmSubdomainMatrixManager_v2>();
			this.subdomainMatrixAssemblers = new ConcurrentDictionary<int, IDomainMatrixAssembler_v2<TMatrix>>();
			this.subdomainVectorsPsm = new ConcurrentDictionary<int, PsmSubdomainVectors_v2>();
			environment.DoPerNode(subdomainID =>
			{
				ISubdomain_v2 subdomain = partition.GetSubdomain(subdomainID);
				var subLinearSystem = new SubdomainLinearSystem_v2<TMatrix>(LinearSystem, subdomainID);
				IDomainMatrixAssembler_v2<TMatrix> matrixAssembler = matrixManagerFactory.CreateAssembler();

				var dofManager = cacheElementDofs
					? new MonolithicDomainDofManagerCaching(subdomain, dofOrderingStrategy, null)
					: new MonolithicDomainDofManager(subdomain, dofOrderingStrategy, null);

				var psmDofs = new PsmSubdomainDofs_v2(partition, subdomain, dofManager, false);
				IPsmSubdomainMatrixManager_v2 psmMatrices = matrixManagerFactory.CreateMatrixManager(provider, subLinearSystem, psmDofs);
				var psmVectors = new PsmSubdomainVectors_v2(subLinearSystem, psmDofs, psmMatrices);

				subdomainLinearSystems[subdomainID] = subLinearSystem;
				subdomainDofsAll[subdomainID] = dofManager;
				subdomainMatrixAssemblers[subdomainID] = matrixAssembler;
				subdomainDofsPsm[subdomainID] = psmDofs;
				subdomainMatricesPsm[subdomainID] = psmMatrices;
				subdomainVectorsPsm[subdomainID] = psmVectors;
			});

			if (isHomogeneous)
			{
				this.scaling = new HomogeneousScaling_v2(environment, s => subdomainDofsPsm[s]/*, reanalysis*/);
			}
			else
			{
				throw new NotImplementedException();
				//this.scaling = new HeterogeneousScaling(environment, subdomainTopology,
				//	s => algebraicModel.SubdomainLinearSystems[s], s => subdomainDofsPsm[s]);
			}

			if (explicitSubdomainMatrices)
			{
				throw new NotImplementedException();
				//this.interfaceProblemMatrix = new PsmInterfaceProblemMatrixExplicit(
				//	environment, s => subdomainMatricesPsm[s], reanalysis);
			}
			else
			{
				this.interfaceProblemMatrix = new PsmInterfaceProblemMatrixImplicit_v2(environment,
					s => subdomainDofsPsm[s], s => subdomainMatricesPsm[s]);
			}

			//if (reanalysis.RhsVectors)
			//{
			//	this.interfaceProblemVectors = new PsmInterfaceProblemVectorsReanalysis(
			//		environment, subdomainVectors, reanalysis.ModifiedSubdomains);
			//}
			//else
			//{
			this.interfaceProblemVectors = new PsmInterfaceProblemVectors_v2(environment, subdomainVectorsPsm);
			//}

			//if (reanalysis.PreviousSolution)
			//{
			//	//TODO: Refactor this. There must be more than 2 choices. This one here is appropriate for XFEM, but not nonlinear problems.
			//	this.initialSolutionGuessStrategy = 
			//		new SameSolutionAtCommonDofsGuess(environment, reanalysis, s => subdomainDofsPsm[s]);
			//}
			//else
			//{
			this.initialSolutionGuessStrategy = new ZeroInitialSolutionGuess();
			//}

			IPcgResidualConvergence convergenceCriterion;
			if (interfaceProblemSolverFactory.UseObjectiveConvergenceCriterion)
			{
				throw new NotImplementedException();
				//this.objectiveConvergenceCriterion = new ObjectiveConvergenceCriterion_v2<TMatrix>(
				//	environment, algebraicModel, s => subdomainVectors[s]);
				//convergenceCriterion = this.objectiveConvergenceCriterion;
			}
			else
			{
				convergenceCriterion = new DefaultPcgConvergence();
			}
			this.interfaceProblemSolver = interfaceProblemSolverFactory.BuildIterativeMethod(convergenceCriterion);

			Logger = new SolverLogger(GetType().Name);
			LoggerDdm = logger;

			if (provider is ManagedSequentialImplementationProvider)
			{
				directSolverIsParallel = false;
			}
			else
			{
				directSolverIsParallel = true;
			}

			if (optimizedSubdomainTopology)
			{
				throw new NotImplementedException();
			}
			else
			{
				this.subdomainTopology = new SubdomainTopologyGeneral_v2();
				this.subdomainTopology.Initialize(environment, partition, s => subdomainDofsAll[s]);
			}

			analysisIteration = 0;
		}

		public bool CanOverwriteSystemMatrices { get; set; } = true;

		public IDomain Domain { get; }

		public IterativeStatistics InterfaceProblemSolutionStats { get; private set; }

		public LinearSystem_v2 LinearSystem { get; }

		public ISolverLogger Logger { get; }

		public DdmLogger LoggerDdm { get; }

		public string Name => name;

		public IAlgebraicModel_v2 CreateAlgebraicModel(IModel_v2 physicalModel)
		{
			return new DistributedAlgebraicModel_v2(environment, physicalModel, partition, LinearSystem, subdomainDofsAll);
		}

		public void BuildSystemMatrix()
		{
			var globalMatrix = new DistributedOverlappingMatrix<TMatrix>(allDofIndexer);
			environment.DoPerNode(subdomainID =>
			{
				ISubdomain_v2 subdomain = partition.GetSubdomain(subdomainID);
				IMonolithicDofManager subdomainDofs = subdomainDofsAll[subdomainID];
				TMatrix matrix = subdomainMatrixAssemblers[subdomainID].BuildDomainMatrix(subdomain, subdomainDofs);
				globalMatrix.LocalMatrices[subdomainID] = matrix;
			});

			LinearSystem.Matrix = globalMatrix;
		}

		public void PrepareDofs()
		{
			// Dofs of original linear system
			environment.DoPerNode(subdomainID =>
			{
				subdomainDofsAll[subdomainID].PrepareDofs();
				subdomainMatrixAssemblers[subdomainID].HandleDofOrderingWasModified();
			});

			subdomainTopology.FindCommonNodesBetweenSubdomains();
			subdomainTopology.FindCommonDofsBetweenSubdomains();

			allDofIndexer = subdomainTopology.CreateDistributedVectorIndexer(s => subdomainDofsAll[s].DomainDofOrder);
			LinearSystem.RhsVector = new DistributedOverlappingVector(allDofIndexer);
			LinearSystem.Solution = new DistributedOverlappingVector(allDofIndexer);
		}

		public void SolveLinearSystem()
		{
			var watchTotal = new Stopwatch();
			watchTotal.Start();

			if (LoggerDdm != null)
			{
				LoggerDdm.IncrementAnalysisIteration();
			}

			PrepareSubdomainDofs();
			PrepareSubdomainMatrices();
			PrepareGlobal2SubdomainMappings();
			PrepareSubdomainVectors();
			PrepareInterfaceProblem();
			CalcPreconditioner();

			SolveInterfaceProblem();
			RecoverSolution();

			watchTotal.Stop();
			Logger.LogTaskDuration("Solution", watchTotal.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();
			++analysisIteration;
		}

		protected virtual void CalcPreconditioner()
		{
			var watch = new Stopwatch();
			watch.Start();
			preconditioner.Calculate(environment, boundaryDofIndexer, interfaceProblemMatrix);
			watch.Stop();
			Logger.LogTaskDuration("Prepare preconditioner", watch.ElapsedMilliseconds);
		}

		protected bool GuessInitialSolution()
		{
			bool guessIsZero;
			if (analysisIteration == 0)
			{
				(interfaceProblemVectors.InterfaceProblemSolution, guessIsZero) =
					initialSolutionGuessStrategy.GuessFirstSolution(boundaryDofIndexer);
			}
			else
			{
				DistributedOverlappingVector previousSolution = interfaceProblemVectors.InterfaceProblemSolution;
				(interfaceProblemVectors.InterfaceProblemSolution, guessIsZero) =
					initialSolutionGuessStrategy.GuessNextSolution(boundaryDofIndexer, previousSolution);
			}
			interfaceProblemVectors.InterfaceProblemSolution.CacheSendRecvBuffers = cacheDistributedVectorBuffers;
			return guessIsZero;
		}

		protected void LogSizes(IterativeStatistics stats)
		{
			if (LoggerDdm != null)
			{
				var rhsVector = (DistributedOverlappingVector)LinearSystem.RhsVector;
				LoggerDdm.LogSolverConvergenceData(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
				LoggerDdm.LogProblemSize(0, rhsVector.Indexer.NumGlobalIndices);
				LoggerDdm.LogProblemSize(1, boundaryDofIndexer.NumGlobalIndices);

				Dictionary<int, int> subdomainProblemSize = environment.AllGather(
					subdomainID => rhsVector.LocalVectors[subdomainID].Length);
				if (subdomainProblemSize != null)
				{
					foreach (var pair in subdomainProblemSize)
					{
						LoggerDdm.LogSubdomainProblemSize(pair.Key, pair.Value);
					}
				}

				int totalLocalTransfers = environment.AllReduceSum(
					subdomainID => boundaryDofIndexer.CountCommonEntriesOfNodeWithNeighbors(subdomainID).local);
				int totalRemoteTransfers = environment.AllReduceSum(
					subdomainID => boundaryDofIndexer.CountCommonEntriesOfNodeWithNeighbors(subdomainID).remote);
				LoggerDdm.LogTransfers(totalLocalTransfers, totalRemoteTransfers);
			}
		}

		protected void SolveInterfaceProblem()
		{
			var watch = new Stopwatch();
			watch.Start();
			bool initalGuessIsZero = GuessInitialSolution();

			// Solver the interface problem
			IterativeStatistics stats = interfaceProblemSolver.Solve(
				interfaceProblemMatrix.Matrix, preconditioner.Preconditioner, interfaceProblemVectors.InterfaceProblemRhs,
				interfaceProblemVectors.InterfaceProblemSolution, initalGuessIsZero);
			InterfaceProblemSolutionStats = stats;
			watch.Stop();

			Debug.WriteLine("Iterations for boundary problem = " + stats.NumIterationsRequired);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			Logger.LogTaskDuration("Interface problem solution", watch.ElapsedMilliseconds);
			LogSizes(stats);

			//if (objectiveConvergenceCriterion != null)
			//{
			//	Logger.LogTaskDuration("Objective PCG criterion", objectiveConvergenceCriterion.EllapsedMilliseconds);
			//	objectiveConvergenceCriterion.EllapsedMilliseconds = 0;
			//}
		}

		private void PrepareGlobal2SubdomainMappings()
		{
			var watch = new Stopwatch();
			watch.Start();
			this.boundaryDofIndexer = subdomainTopology.CreateDistributedVectorIndexer(
					s => subdomainDofsPsm[s].DofOrderingBoundary);

			// Calculating scaling coefficients
			scaling.CalcScalingMatrices(boundaryDofIndexer);

			watch.Stop();
			Logger.LogTaskDuration("Global-subdomain mappings", watch.ElapsedMilliseconds);
		}

		private void PrepareInterfaceProblem()
		{
			var watch = new Stopwatch();
			watch.Start();
			interfaceProblemMatrix.Calculate(boundaryDofIndexer);
			interfaceProblemVectors.CalcInterfaceRhsVector(boundaryDofIndexer);
			watch.Stop();
			Logger.LogTaskDuration("Prepare interface problem", watch.ElapsedMilliseconds);
		}

		private void PrepareSubdomainDofs()
		{
			var watch = new Stopwatch();
			watch.Start();
			bool isFirstAnalysis = analysisIteration == 0;
			environment.DoPerNode(subdomainID =>
			{
				subdomainDofsPsm[subdomainID].SeparateDofsIntoBoundaryAndInternal();
				subdomainMatricesPsm[subdomainID].ReorderInternalDofs();
			});
			watch.Stop();
			Logger.LogTaskDuration("Subdomain level dofs", watch.ElapsedMilliseconds);
		}

		private void PrepareSubdomainMatrices()
		{
			var watch = new Stopwatch();
			watch.Start();
			bool isFirstAnalysis = analysisIteration == 0;
			environment.DoPerNode(subdomainID =>
			{
				subdomainMatricesPsm[subdomainID].HandleDofsWereModified();
				subdomainMatricesPsm[subdomainID].ExtractKiiKbbKib();
			});

			// Factorize Kii matrices of subdomains
			//TODO: This should be done together with the extraction. However SuiteSparse already uses multiple threads and should
			//		not be parallelized at subdomain level too. Instead environment.DoPerNode should be able to run tasks serially by reading a flag.
			Action<int> factorizeKii = subdomainID =>
			{
				subdomainMatricesPsm[subdomainID].InvertKii();
			};

			if (directSolverIsParallel)
			{
				environment.DoPerNodeSerially(factorizeKii);
			}
			else
			{
				environment.DoPerNode(factorizeKii);
			}

			watch.Stop();
			Logger.LogTaskDuration("Subdomain level matrices", watch.ElapsedMilliseconds);
		}

		private void PrepareSubdomainVectors()
		{
			var watch = new Stopwatch();
			watch.Start();
			bool isFirstAnalysis = analysisIteration == 0;
			environment.DoPerNode(subdomainID =>
			{
				subdomainVectorsPsm[subdomainID].ExtractBoundaryInternalRhsVectors(
						fb => scaling.ScaleBoundaryRhsVector(subdomainID, fb));
			});
			watch.Stop();
			Logger.LogTaskDuration("Subdomain level vectors", watch.ElapsedMilliseconds);
		}

		private void RecoverSolution()
		{
			var watch = new Stopwatch();
			watch.Start();
			environment.DoPerNode(subdomainID =>
			{
				Vector subdomainBoundarySolution = interfaceProblemVectors.InterfaceProblemSolution.LocalVectors[subdomainID];
				subdomainVectorsPsm[subdomainID].CalcStoreSubdomainSolution(subdomainBoundarySolution);
			});
			watch.Stop();
			Logger.LogTaskDuration("Recover solution at all dofs", watch.ElapsedMilliseconds);
		}

		public class Factory
		{
			protected readonly IComputeEnvironment environment;
			protected readonly IImplementationProvider laProvider;

			public Factory(IComputeEnvironment environment, IImplementationProvider laProvider,
				IPsmSubdomainMatrixManagerFactory_v2<TMatrix> matrixManagerFactory)
			{
				this.environment = environment;
				this.laProvider = laProvider;
				PsmMatricesFactory = matrixManagerFactory; //new PsmSubdomainMatrixManagerSymmetricCSparse.Factory();
			}

			public bool CacheElementDofs { get; set; } = true;

			public IDofOrderingStrategy_v2 DofOrderingStrategy { get; set; } = new DefaultDofOrdering(sortNodes: true, sortDofs: true);

			public bool EnableLogging { get; set; } = false;

			public bool ExplicitSubdomainMatrices { get; set; } = false;

			public IPsmInterfaceProblemSolverFactory InterfaceProblemSolverFactory { get; set; } = new PsmInterfaceProblemSolverFactoryPcg();

			public bool IsHomogeneousProblem { get; set; } = true;

			public IPsmSubdomainMatrixManagerFactory_v2<TMatrix> PsmMatricesFactory { get; }

			public IPsmPreconditioner Preconditioner { get; set; } = new PsmPreconditionerIdentity();

			public PsmReanalysisOptions ReanalysisOptions { get; set; } = PsmReanalysisOptions.CreateWithAllDisabled();

			public bool OptimizedSubdomainTopology { get; set; } = false;

			public virtual PsmSolver_v2<TMatrix> CreateSolver(IDomain domain, IPartition_v2 partition)
			{
				//DdmLogger logger = EnableLogging ? new DdmLogger(environment, "PSM Solver", model.NumSubdomains) : null;
				DdmLogger logger = null;
				return new PsmSolver_v2<TMatrix>(environment, domain, partition, laProvider, PsmMatricesFactory,
					ExplicitSubdomainMatrices, Preconditioner, InterfaceProblemSolverFactory, IsHomogeneousProblem,
					logger, OptimizedSubdomainTopology, DofOrderingStrategy, CacheElementDofs);
			}
		}
	}
}
