namespace MGroup.Solvers.DDM.SolversExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class SimpleAnalysis_temp
	{
		private readonly IModel model;
		private readonly ISubstructureSystemSolver solver;

		public SimpleAnalysis_temp(IModel model, ISubstructureSystemSolver solver)
		{
			this.model = model;
			this.solver = solver;
			((GlobalSubstructureProblem)solver.Problem).Model = model;
		}

		public void Run()
		{
			model.ConnectDataStructures();
			solver.PrepareDofs();
			BuildRhs();
			solver.BuildSystemMatrix();
			solver.SolveLinearSystem();
		}

		private void BuildRhs()
		{
			ISubdomain subdomain = model.GetSubdomain(0);
			IEnumerable<NodalLoad> loads = model.EnumerateBoundaryConditions(subdomain.ID)
				.SelectMany(bcSet => bcSet.EnumerateNodalBoundaryConditions(subdomain.EnumerateElements()))
				.OfType<NodalLoad>();
			solver.Problem.AddToSubstructureVector(loads, solver.Problem.SystemRhs);
		}
	}
}
