namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.Results;

	public class MatrixFreeAlgebraicModel : IAlgebraicModel_v2
	{
		private readonly IComputeEnvironment environment;
		private readonly LinearSystem_v2 linearSystem;
		private readonly IDistributedDofManager dofManager;
		private readonly IModel_v2 model;
		private readonly IDomain domain;

		public MatrixFreeAlgebraicModel(IComputeEnvironment environment, IModel_v2 model, IDomain domain,
			LinearSystem_v2 linearSystem, IDistributedDofManager dofManager)
		{
			this.environment = environment;
			this.model = model;
			this.domain = domain;
			this.linearSystem = linearSystem;
			this.dofManager = dofManager;
		}

		public void AddToGlobalVector(IEnumerable<INodalModelQuantity<IDofType>> nodalLoads, IVector vector)
		{
			DistributedOverlappingVector distributedVector = CheckCompatibleVector(vector);
			environment.DoPerNode(elementID =>
			{
				ISuperElement element = domain.GetElement(elementID);
				var elementVector = distributedVector.LocalVectors[elementID];
				IntDofTable elementFreeDofs = dofManager.GetElementDofs(elementID);
				foreach (INodalModelQuantity<IDofType> load in FilterElementData(nodalLoads, element))
				{
					int dofID = model.DofTypes.GetIdOfDof(load.DOF);
					int freeDofIdx = elementFreeDofs[load.Node.ID, dofID];
					elementVector[freeDofIdx] += load.Amount;
				}
			});

			// Nodal loads at the same boundary dof are the same across all elements, so we do not need to sum overlapping entries
		}

		public NodalResults ExtractAllResults(int subdomainID, IVector solutionFreeDofs)
		{
			var results = new Table<int, int, double>();

			DistributedOverlappingVector distributedVector = CheckCompatibleVector(solutionFreeDofs);
			environment.DoPerNode(elementID =>
			{
				// Free dofs
				ISuperElement element = domain.GetElement(elementID);
				Vector elementVector = distributedVector.LocalVectors[elementID];
				IntDofTable elementFreeDofs = dofManager.GetElementDofs(elementID);
				foreach ((int node, int dofID, int dofIdx) in elementFreeDofs)
				{
					// Race condition, but there is no need for sync, since all local vectors will overwrite the same value
					results[node, dofID] = elementVector[dofIdx];
				}
			});

			// Constrained dofs
			ActiveDofs activeDofs = model.DofTypes;
			foreach (INodalDirichletBoundaryCondition<IDofType> constraint in model.GetDirichletBCs())
			{
				results[constraint.Node.ID, activeDofs.GetIdOfDof(constraint.DOF)] = constraint.Amount;
			}

			return new NodalResults(results);
		}

		internal DistributedOverlappingVector CheckCompatibleVector(IVector vector)
		{
			var rhs = (DistributedOverlappingVector)linearSystem.RhsVector;
			if (vector is DistributedOverlappingVector distributed)
			{
				if (rhs.HasSameFormat(distributed))
				{
					return distributed;
				}
			}

			throw new NonMatchingFormatException(
				"The provided vector has a different format than the current distributed linear system."
				+ $" Ensure it was created by this linear system object.");
		}

		private IEnumerable<Tbc> FilterElementData<Tbc>(IEnumerable<Tbc> nodalBCs, ISuperElement element)
			where Tbc : INodalModelQuantity<IDofType>
		{
			var elementNodes = new HashSet<INode>(element.EnumerateNodes());
			return nodalBCs.Where(bc => elementNodes.Contains(bc.Node));
		}
	}
}
