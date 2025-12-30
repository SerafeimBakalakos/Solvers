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

	public class ElementDiagonalPreconditionerGlobal : IMatrixFreePreconditioner
	{
		private IDofScaling dofScaling;
		private ISubdomainDofOrdering_v2 dofOrdering;
		private IReadOnlyCollection<ISuperElement> elements;
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;

		public ElementDiagonalPreconditionerGlobal()
		{
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

		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering, IDofScaling dofScaling)
		{
			if (systemMatrix is PartitionedMatrixGlobal partitionedMatrix)
			{
				this.elements = elements;
				this.dofOrdering = dofOrdering;
				this.dofScaling = dofScaling;
				dofScaling.Initialize();

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

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) => throw new NotImplementedException();
	}
}
