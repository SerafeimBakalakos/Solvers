namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;

	public class SimpleAnalysis_temp
	{
		private readonly IModel_v2 model;
		private readonly IAlgebraicModel_v2 algebraicModel;
		private readonly ISolver_v2 solver;

		public SimpleAnalysis_temp(IModel_v2 model, IAlgebraicModel_v2 algebraicModel, ISolver_v2 solver)
		{
			this.model = model;
			this.algebraicModel = algebraicModel;
			this.solver = solver;
		}

		public void Run()
		{
			solver.PrepareDofs();
			BuildRhs();
			solver.BuildSystemMatrix();
			solver.SolveLinearSystem();
		}

		private void BuildRhs()
		{
			IEnumerable<INodalNeumannBoundaryCondition<IDofType>> neumannBCs = model.GetNeumannBCs();
			algebraicModel.AddToGlobalVector(neumannBCs, solver.LinearSystem.RhsVector);
			//algebraicModel.AddToSubdomainVector(neumannBCs, solver.Problem.SystemRhs);
		}
	}
}
