namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Results;

	public class SharedMemoryAlgebraicModel_v2 : IAlgebraicModel_v2
	{
		private readonly ISubdomainDofOrdering_v2 dofOrdering;

		public SharedMemoryAlgebraicModel_v2(IModel_v2 model, ISubdomainDofOrdering_v2 dofOrdering)
		{
			Model = model;
			this.dofOrdering = dofOrdering;
		}

		public IModel_v2 Model { get; }

		public void AddToSubdomainVector(IEnumerable<INodalModelQuantity<IDofType>> nodalModelQuantities, IVector vector)
		{
			Vector subdomainVector = CheckCompatibleVector(vector);
			foreach (INodalModelQuantity<IDofType> nodalQuantity in nodalModelQuantities)
			{
				int dofID = Model.DofTypes.GetIdOfDof(nodalQuantity.DOF);
				int dofIdx = dofOrdering.Dofs[nodalQuantity.Node.ID, dofID];
				subdomainVector[dofIdx] += nodalQuantity.Amount;
			}
		}

		public NodalResults ExtractAllResults(IVector solutionFreeDofs)
		{
			CheckCompatibleVector(solutionFreeDofs);
			var results = new Table<int, int, double>();

			// Free dofs
			foreach ((int node, int dof, int freeDofIdx) in dofOrdering.Dofs)
			{
				results[node, dof] = solutionFreeDofs[freeDofIdx];
			}

			// Constrained dofs
			ActiveDofs activeDofs = Model.DofTypes;
			IEnumerable<INodalDirichletBoundaryCondition<IDofType>> constraints = Model.FindDirichletBCsOfSubdomain(0);
			foreach (var constraint in constraints)
			{
				results[constraint.Node.ID, activeDofs.GetIdOfDof(constraint.DOF)] = constraint.Amount;
			}

			return new NodalResults(results);
		}

		internal Vector CheckCompatibleVector(IVector vector)
		{
			// Casting inside here is usually safe since all global vectors should be created by this object
			if ((vector is Vector casted) && (vector.Length == dofOrdering.NumDofs))
			{
				return casted;
			}

			throw new NonMatchingFormatException("The provided vector has a different format than the current linear system."
				+ $" Make sure it was created by this linear system object.");
		}
	}
}
