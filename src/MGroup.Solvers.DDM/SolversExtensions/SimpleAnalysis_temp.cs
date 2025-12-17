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
		private readonly IAlgebraicModel_v2 algebraicModel;
		private readonly ISubdomainSystemSolver solver;

		public SimpleAnalysis_temp(IModel_v2 model, IAlgebraicModel_v2 algebraicModel, ISubdomainSystemSolver solver)
		{
			this.model = model;
			this.algebraicModel = algebraicModel;
			this.solver = solver;
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
			algebraicModel.AddToGlobalVector(neumannBCs, solver.LinearSystem.RhsVector);
			//algebraicModel.AddToSubdomainVector(neumannBCs, solver.Problem.SystemRhs);
		}
	}
}
