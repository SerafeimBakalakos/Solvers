namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers;

	public interface ISubdomain_v2
	{
		IEnumerable<ISuperElement> EnumerateSuperElements();

		int GetMultiplicityOfNode(int nodeID);

		IntDofTable OrderDofs();
	}
}
