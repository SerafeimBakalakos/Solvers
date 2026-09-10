namespace MGroup.Solvers.Multigrid.Tests.GmgSolver
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Multigrid.CycleSchedules;
	using MGroup.Solvers.Multigrid.DirectSolver;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Reordering;
	using MGroup.Solvers.Multigrid.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Examples;

	public static class Cantilever2DTest
	{
		private static void RunTest()
		{
			int[] numElements = [81, 17];

			var example = new CantileverQuad4ElasticExample();
			Model model = example.CreateFemModel(numElements[0], numElements[1]);

			var grid = new Grid2D(numElements[0] + 1, numElements[1] + 1);
			var prolongation = new Prolongation2DVectorStrategy();
			var linearAlgebraProvider = new ManagedSequentialImplementationProvider();
			var solverBuilder = new GmgSolverBuilder(numLevels: 3, grid, prolongation, maxCycles: 100, linearAlgebraProvider);
			solverBuilder.CycleSchedule = new VCycleSchedule();
			var smoother = new StationaryIterationSmoother(new GaussSeidelIterationCsr(), 2);
			solverBuilder.SetSmoothers(smoother);
			solverBuilder.CoarsestSystemSolver = new CholeskyCscCoarseSolver(linearAlgebraProvider, new AmdSymmetricOrdering());
			solverBuilder.ResidualTolerance = 1E-8;
		}
	}
}
