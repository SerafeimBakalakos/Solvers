namespace MGroup.Solvers.DDM.Partitioning
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Discretization;

	public interface IPartition_v2
	{
		IEnumerable<ISubdomain_v2> Subdomains { get; }

		bool DoesSubdomainContainNode(int nodeID, int subdomainID);

		IEnumerable<ISubdomain_v2> EnumerateSubdomainsOfNode(INode node);

		int FindMultiplicityOfNode(int nodeID);

		ISubdomain_v2 GetSubdomain(int subdomainID);
	}
}
