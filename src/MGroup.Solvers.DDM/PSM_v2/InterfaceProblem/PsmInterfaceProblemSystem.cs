using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MGroup.LinearAlgebra.Matrices;
using MGroup.LinearAlgebra.Vectors;
using MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition;

namespace MGroup.Solvers.DDM.PSM_v2.InterfaceProblem
{
	public class PsmInterfaceProblemSystem : ISubstructureProblemSystem
	{
		public ISubstructureDofOrdering DofOrdering { get; set; }

		public IMatrix Matrix { get; set; }

		public IVector Rhs { get; set; }

		public IVector Solution { get; set; }

		public ISubstructure Substructure { get; set; }

		public void BuildMatrix()
		{
			throw new NotImplementedException();
		}

		public void BuildRhs()
		{
			throw new NotImplementedException();
		}

		public void FindDofs()
		{
			throw new NotImplementedException();
		}
	}
}
