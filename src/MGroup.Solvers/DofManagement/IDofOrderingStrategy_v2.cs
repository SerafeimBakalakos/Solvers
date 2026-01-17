namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Discretization;

	public interface IDofOrderingStrategy_v2
	{
		IntDofTable OrderDomainDofs(IDomain domain, Func<int, IntDofTable> getElementDofs);
	}
}
