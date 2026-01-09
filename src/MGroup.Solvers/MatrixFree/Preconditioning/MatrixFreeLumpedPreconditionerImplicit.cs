namespace MGroup.Solvers.MatrixFree.Preconditioning
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.MatrixFree.Dofs;

	public class MatrixFreeLumpedPreconditionerImplicit : IPreconditioner
	{
		private readonly IComputeEnvironment environment;
		private readonly IDofScaling dofScaling;
		private readonly bool cacheTempStorage;
		
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;
		private DistributedOverlappingIndexer indexer;
		private DistributedOverlappingVector tempVectors;

		public MatrixFreeLumpedPreconditionerImplicit(IComputeEnvironment environment, IDofScaling dofScaling, bool cacheTempStorage)
		{
			this.environment = environment;
			this.dofScaling = dofScaling;
			this.cacheTempStorage = cacheTempStorage;
		}

		public IPreconditioner CopyWithInitialSettings() => throw new NotImplementedException();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			if (rhsVector is DistributedOverlappingVector rhsCasted && lhsVector is DistributedOverlappingVector lhsCasted)
			{
				SolveLinearSystem(rhsCasted, lhsCasted);
			}
			else
			{
				throw new ArgumentException(
					"This operation is legal only if the left-hand-side and right-hand-side vectors are distributed" +
					" with overlapping entries.");
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			if (matrix is DistributedOverlappingMatrix<IMatrix> distributedMatrix)
			{
				UpdateMatrix(distributedMatrix);
			}
			else
			{
				throw new NonMatchingFormatException($"Can only operate on {nameof(DistributedOverlappingMatrix<IMatrix>)}");
			}
		}

		private void SolveLinearSystem(DistributedOverlappingVector input, DistributedOverlappingVector output)
		{
			Debug.Assert(indexer.IsCompatibleWith(input.Indexer));
			Debug.Assert(indexer.IsCompatibleWith(output.Indexer));

			environment.DoPerNode(elementID =>
			{
				Vector xe = input.LocalVectors[elementID];
				Vector ye = output.LocalVectors[elementID];
				DiagonalMatrix De = elementInverseDiagonals[elementID];
				DiagonalMatrix We = dofScaling.GetScalingMatrix(elementID);

				Vector temp = cacheTempStorage ? tempVectors.LocalVectors[elementID] : Vector.CreateZero(xe.Length);

				// ye = (We)^T * De * We * xe
				We.MultiplyIntoResult(xe, ye);
				De.MultiplyIntoResult(ye, temp);
				We.MultiplyIntoResult(temp, ye); // Don't transpose We, since it is diagonal
			});
			
			output.SumOverlappingEntries();
		}

		private void UpdateMatrix(DistributedOverlappingMatrix<IMatrix> matrix)
		{
			indexer = matrix.Indexer;
			dofScaling.Update();

			elementInverseDiagonals = environment.CalcNodeData(elementID =>
			{
				IMatrix elementMatrix = matrix.LocalMatrices[elementID];
				// Extracting the diagonal is not implicit per se, but it is always more efficient than repeatedly accessing the diagonal entries of an arbitrary matrix format.
				var diagonal = DiagonalMatrix.CreateFromArray(elementMatrix.GetDiagonalAsArray()); 
				diagonal.Invert();
				return diagonal;
			});

			if (cacheTempStorage)
			{
				tempVectors = new DistributedOverlappingVector(indexer);
			}
		}

		public class Factory : IMatrixFreePreconditionerFactory
		{
			private readonly IComputeEnvironment environment;

			public Factory(IComputeEnvironment environment)
			{
				this.environment = environment;
			}

			public bool CacheTemporaryStorage { get; set; } = false;

			public IPreconditioner CreatePreconditioner(IDofScaling dofScaling)
				=> new MatrixFreeLumpedPreconditionerImplicit(environment, dofScaling, CacheTemporaryStorage);
		}
	}
}
