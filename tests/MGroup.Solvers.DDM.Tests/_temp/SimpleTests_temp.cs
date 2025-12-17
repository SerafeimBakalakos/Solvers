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
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.MSolve.Solution;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.Psm;
	using MGroup.Solvers.DDM.PSM.InterfaceProblem;
	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
	using MGroup.Solvers.DDM.SolversExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.Direct;
	using MGroup.Solvers.DDM.SolversExtensions.Iterative;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM;
	using MGroup.Solvers.DDM.Tests.ExampleModels;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Results;

	using Xunit;

	public static class SimpleTests_temp
	{
		public enum SolverName
		{
			DenseMatrixSolver, CholeskyCscSolver, PcgSolver
		}

		[Theory]
		[InlineData(SolverName.DenseMatrixSolver)]
		[InlineData(SolverName.CholeskyCscSolver)]
		[InlineData(SolverName.PcgSolver)]
		public static void TestPlane2D(SolverName solverName)
		{
			IComputeEnvironment environment = new SequentialSharedEnvironment();
			IImplementationProvider laProviderForSolver = new ManagedSequentialImplementationProvider();

			// Environment
			ComputeNodeTopology nodeTopology = Plane2DExample.CreateNodeTopology();
			environment.Initialize(nodeTopology);

			// Model
			IModel_v2 model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());

			// Constitutive problem
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Solver
			ISubdomainSystemSolver solver = CreateSolver(solverName, environment, laProviderForSolver, model, elementMatrixProvider);
			IAlgebraicModel_v2 algebraicModel = solver.CreateAlgebraicModel(model);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, algebraicModel, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.DofTypes);
			double tolerance = 1E-7;
			NodalResults computedResults = algebraicModel.ExtractAllResults(solver.LinearSystem.Solution);
			Assert.True(expectedResults.IsSuperSetOf(computedResults, tolerance, out string msg), msg);

			//Debug.WriteLine($"Num PCG iterations = {solver.PcgStats.NumIterationsRequired}," +
			//    $" final residual norm ratio = {solver.PcgStats.ResidualNormRatioEstimation}");

			// Check convergence
			//int precision = 10;
			//int pcgIterationsExpected = 63;
			//double pcgResidualNormRatioExpected = 4.859075883397028E-11;
			//IterativeStatistics stats = solver.InterfaceProblemSolutionStats;
			//Assert.Equal(pcgIterationsExpected, stats.NumIterationsRequired);
			//Assert.Equal(pcgResidualNormRatioExpected, stats.ResidualNormRatioEstimation, precision);
		}

		private static ISubdomainSystemSolver CreateSolver(SolverName solverName, IComputeEnvironment environment, IImplementationProvider laProviderForSolver, IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			if (solverName == SolverName.DenseMatrixSolver)
			{
				var subdomain = new FullDomain_temp(model, elementMatrixProvider);
				return new DenseMatrixSolver_v2(subdomain, true);
			}
			else if (solverName == SolverName.CholeskyCscSolver)
			{
				var subdomain = new FullDomain_temp(model, elementMatrixProvider);
				return new CholeskyCscSolver_v2(subdomain, laProviderForSolver);
			}
			else if (solverName == SolverName.PcgSolver)
			{
				var subdomain = new FullDomain_temp(model, elementMatrixProvider);
				var pcgAlgorithmFactory = new PcgAlgorithm.Factory();
				pcgAlgorithmFactory.MaxIterationsProvider = new FixedMaxIterationsProvider(100);
				pcgAlgorithmFactory.ResidualTolerance = 1E-10;
				var preconditioner = new JacobiPreconditioner();
				return new PcgSolver_v2(subdomain, pcgAlgorithmFactory.Build(), preconditioner);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
