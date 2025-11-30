namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DDM.DiscretizationExtensions;
	using MGroup.Solvers.DDM.SolversExtensions.DofOrdering;

	public class GlobalSubstructureProblem : ISubstructureProblem
	{
		private IModel model;
		private GlobalSubstructureVectorAssembler_v2 vectorAssembler;

		public GlobalSubstructureProblem(ISubstructure substructure, ISubstructureDofOrdering dofOrdering)
		{
			Substructure = substructure;
			DofOrdering = dofOrdering;
		}

		public ISubstructureDofOrdering DofOrdering { get; }

		public IModel Model
		{
			get => model;
			set
			{
				model = value;
				vectorAssembler = new GlobalSubstructureVectorAssembler_v2(model.GetActiveDofs_temp());
			}
		}

		public ISubstructure Substructure { get; }

		public IMatrix SystemMatrix { get; set; }

		public IVector SystemRhs { get; set; }

		public IVector SystemSolution { get; set; }

		public void AddToSubstructureVector(IEnumerable<INodalModelQuantity<IDofType>> nodalModelQuantities, IVector vector)
		{
			Vector substructureVector = CheckCompatibleVector(vector);
			vectorAssembler.AddToSubstructureVector(nodalModelQuantities, substructureVector, DofOrdering);
		}

		public void OrderDofs()
		{
			DofOrdering.OrderDofs();
			DofOrdering.PrepareDofMaps();

			SystemMatrix = null;
			SystemRhs = Vector.CreateZero(DofOrdering.NumDofs);
			SystemSolution = Vector.CreateZero(DofOrdering.NumDofs);
		}

		internal Vector CheckCompatibleVector(IVector vector)
		{
			// Casting inside here is usually safe since all global vectors should be created by this object
			if ((vector is Vector casted) && (vector.Length == DofOrdering.NumDofs))
			{
				return casted;
			}

			throw new NonMatchingFormatException("The provided vector has a different format than the current linear system."
				+ $" Make sure it was created by this linear system object.");
		}
	}
}
