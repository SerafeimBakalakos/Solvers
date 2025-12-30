namespace MGroup.Solvers.DDM.Tests._temp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.Providers;
	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.MSolve.Solution;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.Partitioning;
	using MGroup.Solvers.DDM.Psm;
	using MGroup.Solvers.DDM.PSM.InterfaceProblem;
	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
	using MGroup.Solvers.DDM.Tests.ExampleModels;
	using MGroup.Solvers.Direct;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Iterative;
	using MGroup.Solvers.MatrixFree;
	using MGroup.Solvers.Results;
	using Xunit;

	public static class SimpleTests_temp
	{
		public enum SolverName
		{
			DenseMatrixSolver, CholeskyCscSolver, PcgSolver
		}

		[Fact]
		public static void TestPsmSolver()
		{
			// Environment
			IComputeEnvironment environment = new SequentialSharedEnvironment();
			IImplementationProvider laProviderForSolver = new ManagedSequentialImplementationProvider();
			ComputeNodeTopology nodeTopology = Plane2DExample.CreateNodeTopology();
			environment.Initialize(nodeTopology);

			// Model
			var model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());
			model.ConnectDataStructures();
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Partition
			var domain = new FullDomain_temp(model, elementMatrixProvider);
			Dictionary<int, int> elementsToSubdomains = Plane2DExample.GetSubdomainsOfElements();
			var partitioner = new SimplePartitioner_temp(Plane2DExample.NumSubdomainsTotal, e => elementsToSubdomains[e]);
			IPartition_v2 partition = partitioner.Decompose(model, elementMatrixProvider);

			// Solver
			var solverFactory = new PsmSolver_v2<SymmetricCscMatrix>.Factory(environment, laProviderForSolver, new PsmSubdomainMatrixManagerSymmetricCsc_v2.Factory());
			solverFactory.InterfaceProblemSolverFactory = new PsmInterfaceProblemSolverFactoryPcg()
			{
				MaxIterations = 200,
				ResidualTolerance = 1E-10
			};
			PsmSolver_v2<SymmetricCscMatrix> solver = solverFactory.BuildSolver(domain, partition);
			IAlgebraicModel_v2 algebraicModel = solver.CreateAlgebraicModel(model);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, algebraicModel, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.DofTypes);
			double tolerance = 1E-7;
			environment.DoPerNode(subdomainID =>
			{
				NodalResults computedResults = algebraicModel.ExtractAllResults(subdomainID, solver.LinearSystem.Solution);
				Assert.True(expectedResults.IsSuperSetOf(computedResults, tolerance, out string msg), msg);
			});

			// Check convergence
			int precision = 10;
			int pcgIterationsExpected = 63;
			double pcgResidualNormRatioExpected = 4.859075883397028E-11;
			IterativeStatistics stats = solver.InterfaceProblemSolutionStats;
			Assert.Equal(pcgIterationsExpected, stats.NumIterationsRequired);
			Assert.Equal(pcgResidualNormRatioExpected, stats.ResidualNormRatioEstimation, precision);
		}

		[Fact]
		public static void TestMatrixFreeSolver()
		{
			// Model
			IModel_v2 model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());
			model.ConnectDataStructures();

			// Constitutive problem
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Environment
			IComputeEnvironment environment = new SequentialSharedEnvironment();
			var nodeTopology = new ComputeNodeTopology();
			foreach (IElementType element in model.EnumerateElements())
			{
				var neighbors = new HashSet<int>();
				foreach (INode node in element.Nodes)
				{
					neighbors.UnionWith(node.ElementsDictionary.Values.Select(e => e.ID));
				}
				neighbors.Remove(element.ID);

				nodeTopology.AddNode(element.ID, neighbors.ToArray(), 0);
			}
			environment.Initialize(nodeTopology);

			// Solver
			var domain = new FullDomain_temp(model, elementMatrixProvider);
			var pcgAlgorithmFactory = new PcgAlgorithm.Factory();
			pcgAlgorithmFactory.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
			pcgAlgorithmFactory.ResidualTolerance = 1E-10;
			pcgAlgorithmFactory.Logger = new PcgDebugLogger_v2();
			IPreconditioner preconditioner = new IdentityPreconditioner();
			//IPreconditioner preconditioner = new PartitionedJacobiPreconditionerGlobal();
			var partition = new DefaultElementPartition(environment, model, domain);
			//IPreconditioner preconditioner = new ElementDiagonalPreconditionerGlobal(new HomogeneousDofScaling(partition));
			var solver = new MatrixFreeSolverDistributed(environment, domain, partition, pcgAlgorithmFactory.Build(), preconditioner);
			IAlgebraicModel_v2 algebraicModel = solver.CreateAlgebraicModel(model);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, algebraicModel, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.DofTypes);
			double tolerance = 1E-7;
			NodalResults computedResults = algebraicModel.ExtractAllResults(0, solver.LinearSystem.Solution);
			Assert.True(expectedResults.IsSuperSetOf(computedResults, tolerance, out string msg), msg);

			// Check convergence
			int precision = 10;
			int pcgIterationsExpected = 86;
			double pcgResidualNormRatioExpected = 8.3702031765832112E-11;
			IterativeStatistics stats = solver.IterativeAlgorithmStats;
			Assert.True(stats.NumIterationsRequired <= pcgIterationsExpected);
			Assert.Equal(pcgResidualNormRatioExpected, stats.ResidualNormRatioEstimation, precision);
		}

		[Fact]
		public static void TestMatrixFreeSolverGlobal()
		{
			IComputeEnvironment environment = new SequentialSharedEnvironment();

			// Model
			IModel_v2 model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());
			model.ConnectDataStructures();

			// Constitutive problem
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Solver
			var domain = new FullDomain_temp(model, elementMatrixProvider);
			var pcgAlgorithmFactory = new PcgAlgorithm.Factory();
			pcgAlgorithmFactory.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
			pcgAlgorithmFactory.ResidualTolerance = 1E-10;
			//pcgAlgorithmFactory.Logger = new PcgDebugLogger_v2();
			IPreconditioner preconditioner = new IdentityPreconditioner();
			//IPreconditioner preconditioner = new PartitionedJacobiPreconditionerGlobal();
			var partition = new DefaultElementPartition(environment, model, domain);
			//IPreconditioner preconditioner = new ElementDiagonalPreconditionerGlobal(new HomogeneousDofScaling(partition));
			var solver = new MatrixFreeSolverGlobal(domain, pcgAlgorithmFactory.Build(), preconditioner);
			IAlgebraicModel_v2 algebraicModel = solver.CreateAlgebraicModel(model);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, algebraicModel, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.DofTypes);
			double tolerance = 1E-7;
			NodalResults computedResults = algebraicModel.ExtractAllResults(0, solver.LinearSystem.Solution);
			Assert.True(expectedResults.IsSuperSetOf(computedResults, tolerance, out string msg), msg);

			// Check convergence
			int precision = 10;
			int pcgIterationsExpected = 86;
			double pcgResidualNormRatioExpected = 8.3702031765832112E-11;
			IterativeStatistics stats = solver.IterativeAlgorithmStats;
			Assert.True(stats.NumIterationsRequired <= pcgIterationsExpected);
			Assert.Equal(pcgResidualNormRatioExpected, stats.ResidualNormRatioEstimation, precision);
		}

		[Theory]
		[InlineData(SolverName.DenseMatrixSolver)]
		[InlineData(SolverName.CholeskyCscSolver)]
		[InlineData(SolverName.PcgSolver)]
		public static void TestMonolithicSolvers(SolverName solverName)
		{
			// Model
			IModel_v2 model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());
			model.ConnectDataStructures();

			// Constitutive problem
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Solver
			var domain = new FullDomain_temp(model, elementMatrixProvider);
			ISolver_v2 solver = CreateMonolithicSolver(solverName, domain);
			IAlgebraicModel_v2 algebraicModel = solver.CreateAlgebraicModel(model);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, algebraicModel, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.DofTypes);
			double tolerance = 1E-7;
			NodalResults computedResults = algebraicModel.ExtractAllResults(0, solver.LinearSystem.Solution);
			Assert.True(expectedResults.IsSuperSetOf(computedResults, tolerance, out string msg), msg);
		}

		private static ISolver_v2 CreateMonolithicSolver(SolverName solverName, ISubdomain_v2 domain)
		{
			if (solverName == SolverName.DenseMatrixSolver)
			{
				return new DenseMatrixSolver_v2(domain, true);
			}
			else if (solverName == SolverName.CholeskyCscSolver)
			{
				IImplementationProvider laProviderForSolver = new ManagedSequentialImplementationProvider();
				return new CholeskyCscSolver_v2(domain, laProviderForSolver);
			}
			else if (solverName == SolverName.PcgSolver)
			{
				var pcgAlgorithmFactory = new PcgAlgorithm.Factory();
				pcgAlgorithmFactory.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
				pcgAlgorithmFactory.ResidualTolerance = 1E-10;
				//pcgAlgorithmFactory.Logger = new PcgDebugLogger_v2();
				//var preconditioner = new IdentityPreconditioner();
				var preconditioner = new JacobiPreconditioner();
				return new PcgSolver_v2(domain, pcgAlgorithmFactory.Build(), preconditioner);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
