namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Discretization;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearAlgebraExtensions;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.Preconditioning;

	public class MatrixFreeLumpedPreconditionerMonolithic : IMatrixFreePreconditionerMonolithic
	{
		private IDofScaling dofScaling;
		private IMonolithicDofManager dofManager;
		private IReadOnlyCollection<ISuperElement> elements;
		private Dictionary<int, DiagonalMatrix> elementInverseDiagonals;

		public MatrixFreeLumpedPreconditionerMonolithic()
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
				int[] elementToDomainDofs = dofManager.MapDofsElementToDomain(element);
				DiagonalMatrix We = dofScaling.GetScalingMatrix(element.ID);
				Vector ye = y.GetSubvector(elementToDomainDofs);
				var xe = Vector.CreateZero(elementToDomainDofs.Length);
				elementInverseDiagonals[element.ID].MultiplyIntoResult(We*ye, xe);
				x.AddIntoThisNonContiguouslyFrom(elementToDomainDofs, We*xe);
			}
		}

		public void Update(IReadOnlyMatrix systemMatrix, IReadOnlyCollection<ISuperElement> elements, IMonolithicDofManager dofManager, IDofScaling dofScaling)
		{
			if (systemMatrix is ElementWiseMatrixMonolithic partitionedMatrix)
			{
				this.elements = elements;
				this.dofManager = dofManager;
				this.dofScaling = dofScaling;
				dofScaling.Update();

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
				throw new NonMatchingFormatException($"Can only operate on {nameof(ElementWiseMatrixMonolithic)}");
			}
		}

		public void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified) => throw new NotImplementedException();
	}
}
