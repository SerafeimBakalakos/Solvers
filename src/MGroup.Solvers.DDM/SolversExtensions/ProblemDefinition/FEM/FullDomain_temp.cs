namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;

	public class FullDomain_temp : DefaultSubstructure_temp
	{
		private readonly IModel model;

		public FullDomain_temp(IModel model)
			: base(model.GetSubdomain(0), model)
		{
			this.model = model;
		}
	}
}
