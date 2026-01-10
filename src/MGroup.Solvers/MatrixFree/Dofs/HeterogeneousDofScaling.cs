namespace MGroup.Solvers.MatrixFree.Dofs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Distributed;
	using MGroup.Solvers.LinearSystem;

	public class HeterogeneousDofScaling : IDofScaling
	{
		private readonly ISubdomain_v2 domain;
		private readonly IComputeEnvironment environment;
		private readonly LinearSystem_v2 linearSystem;
		private Dictionary<int, DiagonalMatrix> elementScalingMatrices;

		public HeterogeneousDofScaling(IComputeEnvironment environment, ISubdomain_v2 domain, LinearSystem_v2 linearSystem)
		{
			this.environment = environment;
			this.domain = domain;
			this.linearSystem = linearSystem;
		}

		public DiagonalMatrix GetScalingMatrix(int elementID) => elementScalingMatrices[elementID];

		public void Update()
		{
			var domainMatrix = (DistributedOverlappingMatrix<IMatrix>)linearSystem.Matrix;

			// Extract the diagonals of element matrices
			Dictionary<int, Vector> elementMatrixDiagonals = environment.CalcNodeData(elementID =>
			{
				ISuperElement element = domain.GetElement(elementID);
				IMatrix elementMatrix = domainMatrix.LocalMatrices[elementID];
				return elementMatrix.GetDiagonal();
			});

			// Leverage DistributedOverlappingVector to communicate the stiffnesses (or whatever the matrix shows) at common dofs between elements and find the relative values
			DistributedOverlappingIndexer indexer = domainMatrix.Indexer;
			var distributedVector = new DistributedOverlappingVector(indexer, elementMatrixDiagonals);
			distributedVector.RegularizeOverlappingEntries_v2();

			// Store them
			elementScalingMatrices = environment.CalcNodeData(elementID =>
			{
				Vector elementDiagonal = distributedVector.LocalVectors[elementID];
				return DiagonalMatrix.CreateFromArray(elementDiagonal.RawData);
			});
		}
	}
}
