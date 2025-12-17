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
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.NumericalAnalyzers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.LinearSystem;
	using MGroup.Solvers.DDM.Psm;
	using MGroup.Solvers.DDM.PSM.InterfaceProblem;
	using MGroup.Solvers.DDM.PSM.StiffnessMatrices;
	using MGroup.Solvers.DDM.SolversExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.Direct;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM;
	using MGroup.Solvers.DDM.Tests.ExampleModels;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Results;

	using Xunit;

	public static class SimpleTests_temp
	{
		[Fact]
		public static void TestPlane2D()
		{
			IComputeEnvironment environment = new SequentialSharedEnvironment();
			IImplementationProvider laProviderForSolver = new ManagedSequentialImplementationProvider();

			// Environment
			ComputeNodeTopology nodeTopology = Plane2DExample.CreateNodeTopology();
			environment.Initialize(nodeTopology);

			// Model
			IModel_v2 model = new ModelAdapter_temp(Plane2DExample.CreateSingleSubdomainModel());

			// Constituive problem
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();

			// Solver
			var subdomain = new FullDomain_temp(model, elementMatrixProvider);
			using var solver = new CholeskyCscSolver_v2(subdomain, laProviderForSolver);
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
	}
}
