namespace MGroup.Solvers.DDM.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;

	public interface IModel_v2 : IModel
	{
		ActiveDofs DofTypes { get; }

		IEnumerable<INodalNeumannBoundaryCondition<IDofType>> GetNeumannBCs();

		IEnumerable<INodalDirichletBoundaryCondition<IDofType>> GetDirichletBCs();
	}
}
