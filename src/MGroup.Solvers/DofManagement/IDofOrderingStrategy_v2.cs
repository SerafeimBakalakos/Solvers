namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.DiscretizationExtensions;

	public interface IDofOrderingStrategy_v2
	{
		IntDofTable OrderDomainDofs(ISubdomain_v2 domain, Func<int, IntDofTable> getElementDofs);
	}
}
