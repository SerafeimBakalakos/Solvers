namespace MGroup.Solvers.DDM.Partitioning
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.DiscretizationExtensions;

	public class SimplePartitioner_temp
	{
		private readonly int numSubdomains;
		private readonly Func<int, int> getSubdomainOfElement;

		public SimplePartitioner_temp(int numSubdomains, Func<int, int> getSubdomainOfElement)
		{
			this.numSubdomains = numSubdomains;
			this.getSubdomainOfElement = getSubdomainOfElement;
		}

		public IPartition_v2 Decompose(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			var subdomains = new DefaultSubdomain_temp[numSubdomains];
			for (int s = 0; s < numSubdomains; s++)
			{
				subdomains[s] = new DefaultSubdomain_temp(s, model, elementMatrixProvider);
			}

			foreach (IElementType element in model.EnumerateElements())
			{
				int subdomainID = getSubdomainOfElement(element.ID);
				subdomains[subdomainID].AddElement(element);
			}

			return new SharedMemoryPartition(subdomains);
		}
	}
}
