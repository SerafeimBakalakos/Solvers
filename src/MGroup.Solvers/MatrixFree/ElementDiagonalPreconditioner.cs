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

	public class ElementDiagonalPreconditioner : IPreconditioner
	{
		private ISubdomainDofOrdering_v2 dofOrdering;
		private IReadOnlyCollection<ISuperElement> elements;
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;

		public ElementDiagonalPreconditioner()
		{
		}

		public IPreconditioner CopyWithInitialSettings() => throw new NotImplementedException();

		public void SolveLinearSystem(IReadOnlyVector rhsVector, IVector lhsVector)
		{
			//TODO: This is almost idential to PartitionedMatrix.MultiplyVectorIntoResult
			var x = (Vector)lhsVector;
			var y = (Vector)rhsVector;
			foreach (ISuperElement element in elements)
			{
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
				Vector xe = x.GetSubvector(subdomainDofIndices);
				var ye = Vector.CreateZero(subdomainDofIndices.Length);
				elementInverseDiagonals[element.ID].MultiplyIntoResult(xe, ye);
				y.AddIntoThisNonContiguouslyFrom(subdomainDofIndices, ye);
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			if (matrix is PartitionedMatrix partitionedMatrix)
			{
				this.dofOrdering = partitionedMatrix.DofOrdering;
				this.elements = partitionedMatrix.Elements;
				this.elementInverseDiagonals = new Dictionary<int, DiagonalMatrix>();
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
				throw new NonMatchingFormatException($"Can only operate on {nameof(PartitionedMatrix)}");
			}
		}
	}
}
