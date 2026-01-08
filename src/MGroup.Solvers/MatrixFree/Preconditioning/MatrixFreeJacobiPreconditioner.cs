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
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.MatrixFree.Dofs;

	public class MatrixFreeJacobiPreconditioner : IPreconditioner
	{
		private DistributedOverlappingVector inverseDiagonal;

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
			Debug.Assert(inverseDiagonal.HasSameFormat(input));
			Debug.Assert(inverseDiagonal.HasSameFormat(output));

			inverseDiagonal.Environment.DoPerNode(elementID =>
			{
				Vector localX = input.LocalVectors[elementID];
				Vector localY = output.LocalVectors[elementID];
				Vector localDiagonal = inverseDiagonal.LocalVectors[elementID];
				localY.CopyFrom(localX);
				localY.MultiplyEntrywiseIntoThis(localDiagonal);
			});
		}

		private void UpdateMatrix(DistributedOverlappingMatrix<IMatrix> matrix)
		{
			var diagonal = new DistributedOverlappingVector(matrix.Indexer, e => matrix.LocalMatrices[e].GetDiagonal());
			diagonal.SumOverlappingEntries(); // Doing this avoids any need for dof scaling!
			diagonal.DoToAllEntriesIntoThis(x => 1 / x);
			inverseDiagonal = diagonal;
		}

		public class Factory : IMatrixFreePreconditionerFactory
		{
			public IPreconditioner CreatePreconditioner(IDofScaling dofScaling) => new MatrixFreeJacobiPreconditioner();
		}
	}
}
