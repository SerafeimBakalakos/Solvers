using MGroup.Solvers.DiscretizationExtensions;

namespace MGroup.Solvers.DofOrdering
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Reordering;
	using MGroup.Solvers;

	public interface ISubdomainDofOrdering_v2
	{
		IntDofTable DomainDofs { get; }

		int NumDofs { get; }

		/// <summary>
		/// </summary>
		/// <param name="element"></param>
		/// <remarks>
		/// Assumes that all dofs in <paramref name="element"/> also exist in the subdomain.
		/// </remarks>
		/// <returns></returns>
		int[] MapDofsElementToDomain(ISuperElement element);

		void OrderDofs();
	}
}
