namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions;

	public class ElementDiagonalPreconditionerGlobal : IPreconditioner
	{
		private readonly IDofScaling dofScaling;
		private ISubdomainDofOrdering_v2 dofOrdering;
		private IReadOnlyCollection<ISuperElement> elements;
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;

		public ElementDiagonalPreconditionerGlobal(IDofScaling dofScaling)
		{
			this.dofScaling = dofScaling;
		}

		public IPreconditioner CopyWithInitialSettings() => throw new NotImplementedException();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			//TODO: This is almost idential to PartitionedMatrix.MultiplyVectorIntoResult
			var x = (Vector)lhsVector;
			var y = (Vector)rhsVector;
			x.Clear();
			foreach (ISuperElement element in elements)
			{
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
				DiagonalMatrix We = dofScaling.GetScalingMatrix(element.ID);
				Vector ye = y.GetSubvector(subdomainDofIndices);
				var xe = Vector.CreateZero(subdomainDofIndices.Length);
				elementInverseDiagonals[element.ID].MultiplyIntoResult(We*ye, xe);
				x.AddIntoThisNonContiguouslyFrom(subdomainDofIndices, We*xe);
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			if (matrix is PartitionedMatrixGlobal partitionedMatrix)
			{
				dofOrdering = partitionedMatrix.DofOrdering;
				elements = partitionedMatrix.Elements;

				dofScaling.Calculate(partitionedMatrix);
				elementInverseDiagonals = new Dictionary<int, DiagonalMatrix>();
				foreach (ISuperElement element in elements)
				{
					IReadOnlyMatrix elementMatrix = partitionedMatrix.ElementMatrices[element.ID];
					var diagonal = DiagonalMatrix.CreateFromArray(elementMatrix.GetDiagonalAsArray());
					diagonal.Invert();
					elementInverseDiagonals[element.ID] = diagonal;
				}
			}
			else
			{
				throw new NonMatchingFormatException($"Can only operate on {nameof(PartitionedMatrixGlobal)}");
			}
		}
	}
}
