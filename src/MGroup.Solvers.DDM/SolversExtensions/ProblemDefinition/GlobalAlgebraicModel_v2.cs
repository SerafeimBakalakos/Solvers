namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Solvers.DDM.DiscretizationExtensions;

	public class GlobalAlgebraicModel_v2 : IAlgebraicModel_v2
	{
		public GlobalAlgebraicModel_v2(IModel_v2 model, ISubstructureSystemSolver solver)
		{
			LinearSystem = solver.Problem;
			Model = model;
		}

		public ISubstructureProblem LinearSystem { get; }

		public IModel_v2 Model { get; }

		public void AddToSubstructureVector(IEnumerable<INodalModelQuantity<IDofType>> nodalModelQuantities, IVector vector)
		{
			Vector substructureVector = CheckCompatibleVector(vector);
			foreach (INodalModelQuantity<IDofType> nodalQuantity in nodalModelQuantities)
			{
				int dofID = Model.DofTypes.GetIdOfDof(nodalQuantity.DOF);
				int dofIdx = LinearSystem.DofOrdering.Dofs[nodalQuantity.Node.ID, dofID];
				substructureVector[dofIdx] += nodalQuantity.Amount;
			}
		}

		internal Vector CheckCompatibleVector(IVector vector)
		{
			// Casting inside here is usually safe since all global vectors should be created by this object
			if ((vector is Vector casted) && (vector.Length == LinearSystem.DofOrdering.NumDofs))
			{
				return casted;
			}

			throw new NonMatchingFormatException("The provided vector has a different format than the current linear system."
				+ $" Make sure it was created by this linear system object.");
		}
	}
}
