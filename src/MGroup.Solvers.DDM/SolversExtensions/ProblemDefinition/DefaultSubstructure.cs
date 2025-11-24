namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	using TriangleNet.Topology;

	public class DefaultSubstructure //: ISubstructure
	{
		private readonly ActiveDofs activeDofs;

		public DefaultSubstructure(ISubdomain subdomain, ActiveDofs activeDofs)
		{
			Subdomain = subdomain;
			this.activeDofs = activeDofs;
		}

		public ISubdomain Subdomain { get; }

		public IEnumerable<DefaultElement> EnumerateSuperElements()
		{
			foreach (IElementType element in Subdomain.EnumerateElements())
			{
				yield return new DefaultElement(element, activeDofs);
			}
		}

		public IntDofTable GetDofs() => throw new NotImplementedException();
	}
}
