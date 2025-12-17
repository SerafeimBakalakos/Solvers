namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;

	public class FullDomain_temp : DefaultSubdomain_temp
	{
		private readonly IModel_v2 model;

		public FullDomain_temp(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
			: base(model.GetSubdomain(0), model, elementMatrixProvider)
		{
			this.model = model;
		}

		public override int GetMultiplicityOfNode(int nodeID) => 1;
	}
}
