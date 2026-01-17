namespace MGroup.Solvers.DofOrdering_v2
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.DiscretizationExtensions;

	public interface IMonolithicDofManager : IDomainDofManager
	{
		IntDofTable DomainDofOrder { get; }

		int NumDomainDofs { get; }

		/// <summary>
		/// </summary>
		/// <param name="element"></param>
		/// <remarks>
		/// Assumes that all dofs in <paramref name="element"/> also exist in the subdomain.
		/// </remarks>
		/// <returns></returns>
		int[] MapDofsElementToDomain(ISuperElement element);
	}
}
