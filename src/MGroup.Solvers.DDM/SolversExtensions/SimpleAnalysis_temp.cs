namespace MGroup.Solvers.DDM.SolversExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

	public class SimpleAnalysis_temp
	{
		private readonly IModel_v2 model;
		private readonly ISubstructureSystemSolver solver;

		public SimpleAnalysis_temp(IModel_v2 model, ISubstructureSystemSolver solver)
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
			IEnumerable<INodalNeumannBoundaryCondition<IDofType>> neumannBCs = model.FindNeumannBCsOfSubdomain(0);
			solver.Problem.AddToSubstructureVector(neumannBCs, solver.Problem.SystemRhs);
		}
	}
}
