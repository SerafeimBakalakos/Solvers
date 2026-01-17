namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IDomainDofManager
	{
		IntDofTable GetElementDofs(int elementID);

		void PrepareDofs();
	}
}
