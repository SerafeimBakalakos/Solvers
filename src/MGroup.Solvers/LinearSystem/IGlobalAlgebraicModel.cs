namespace MGroup.Solvers.LinearSystem
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.Solvers.DofOrdering;

	public interface IGlobalAlgebraicModel : IAlgebraicModel
	{
		ISubdomainFreeDofOrdering SubdomainFreeDofOrdering { get; }

	}
}
