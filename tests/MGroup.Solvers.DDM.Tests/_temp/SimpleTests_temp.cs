namespace MGroup.Solvers.DDM.Tests._temp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.DataStructures;
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
			IModel model = Plane2DExample.CreateSingleSubdomainModel();
			model.ConnectDataStructures(); //TODOMPI: this is also done in the analyzer

			// Solver
			var substructure = new FullDomain_temp(model);
			var solver = new DenseMatrixSolver_v2(substructure, true);

			// Linear static analysis
			var analysis = new SimpleAnalysis_temp(model, solver);

			// Run the analysis
			analysis.Run();

			// Check results
			NodalResults expectedResults = Plane2DExample.GetExpectedNodalValues(model.GetActiveDofs_temp());
			double tolerance = 1E-7;
			NodalResults computedResults = ExtractResults(model, solver);
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

		private static NodalResults ExtractResults(IModel model, ISubstructureSystemSolver solver)
		{
			var results = new Table<int, int, double>();

			// Free dofs
			foreach ((int node, int dof, int freeDofIdx) in solver.Problem.DofOrdering.Dofs)
			{
				results[node, dof] = solver.Problem.SystemSolution[freeDofIdx];
			}

			// Constrained dofs
			ActiveDofs activeDofs = model.GetActiveDofs_temp();
			var constraints = model.FindDirichletBCsOfSubdomain(0);
			foreach (var constraint in constraints)
			{
				results[constraint.Node.ID, activeDofs.GetIdOfDof(constraint.DOF)] = constraint.Amount;
			}

			return new NodalResults(results);
		}
	}
}
