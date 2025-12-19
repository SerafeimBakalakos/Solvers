namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.DDM.DiscretizationExtensions;

	public class FullDomain_temp : DefaultSubdomain_temp
	{
		private readonly IModel_v2 model;
		private readonly Dictionary<int, ISubdomain_v2> subdomains = new Dictionary<int, ISubdomain_v2>();

		public FullDomain_temp(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
			: base(model.GetSubdomain(0), model, elementMatrixProvider)
		{
			this.model = model;
			foreach (ISubdomain subdomain in model.EnumerateSubdomains())
			{
				subdomains[subdomain.ID] = new DefaultSubdomain_temp(model.GetSubdomain(subdomain.ID), model, elementMatrixProvider);
			}
		}

		public override IEnumerable<INode> EnumerateNodes_temp() => model.EnumerateNodes();

		public override int GetMultiplicityOfNode_temp(int nodeID) => 1;

		public override ISubdomain_v2 GetSubdomain_temp(int subdomainID) => subdomains[subdomainID];
	}
}
