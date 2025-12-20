namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	public interface ISubdomain_v2
	{
		IEnumerable<INode> EnumerateNodes_temp();

		IEnumerable<ISuperElement> EnumerateSuperElements();

		int GetMultiplicityOfNode_temp(int nodeID);

		ISubdomain_v2 GetSubdomain_temp(int subdomainID);

		IntDofTable OrderDofs();
	}
}
