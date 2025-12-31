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

	public class MatrixFreeLumpedPreconditioner : IMatrixFreePreconditioner
	{
		private IDofScaling dofScaling;
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;
		private IComputeEnvironment environment;
		private DistributedOverlappingIndexer indexer;

		public IPreconditioner CopyWithInitialSettings() => new MatrixFreeLumpedPreconditioner();

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
				
				// ye = (We)^T * De * We * xe
				var temp = Vector.CreateZero(xe.Length);
				We.MultiplyIntoResult(xe, ye);
				De.MultiplyIntoResult(ye, temp);
				We.MultiplyIntoResult(temp, ye);
			});

			output.SumOverlappingEntries();
		}

		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering, IDofScaling dofScaling)
		{
			if (systemMatrix is DistributedOverlappingMatrix<IMatrix> distributedMatrix)
			{
				environment = distributedMatrix.Environment;
				indexer = distributedMatrix.Indexer;
				this.dofScaling = dofScaling;
				dofScaling.Initialize();

				elementInverseDiagonals = environment.CalcNodeData(elementID =>
				{
					IMatrix elementMatrix = distributedMatrix.LocalMatrices[elementID];
					var diagonal = DiagonalMatrix.CreateFromArray(elementMatrix.GetDiagonalAsArray());
					diagonal.Invert();
					return diagonal;
				});
			}
			else
			{
				throw new NonMatchingFormatException($"Can only operate on {nameof(DistributedOverlappingMatrix<IMatrix>)}");
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) => throw new NotImplementedException();
	}
}
