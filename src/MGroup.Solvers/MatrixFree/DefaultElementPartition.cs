namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.DiscretizationExtensions;

	public class DefaultElementPartition : IElementPartition
	{
		private readonly IModel_v2 model;
		private readonly FullDomain_temp fullDomain;

		public DefaultElementPartition(IModel_v2 model, FullDomain_temp fullDomain)
		{
			this.model = model;
			this.fullDomain = fullDomain;
		}

		public IEnumerable<ISuperElement> Elements => fullDomain.EnumerateElements();

		public int FindMultiplicityOfNode(int nodeID) => model.GetNode(nodeID).ElementsDictionary.Count;
	}
}
