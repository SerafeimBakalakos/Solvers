namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Reduction;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.MatrixFree.ElementMatrices;

	public class ElementWiseMatrixMonolithic : DefaultMatrix
	{
		public ElementWiseMatrixMonolithic(IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering)
		{
			Elements = elements;
			DofOrdering = dofOrdering;
			NumColumns = dofOrdering.NumDofs;

			ElementMatrices = new Dictionary<int, IReadOnlyMatrix>(elements.Count);
			var converter = new FullRowMajorElementMatrixConverter();
			foreach (ISuperElement element in elements)
			{
				IMatrix elementMatrix = converter.ConvertElementMatrix(element.BuildMatrix());
				ElementMatrices[element.ID] = elementMatrix;
			}
		}

		public override double this[int rowIdx, int colIdx] 
		{ 
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		private ISubdomainDofOrdering_v2 DofOrdering { get; }

		public IReadOnlyCollection<ISuperElement> Elements { get; }

		public Dictionary<int, IReadOnlyMatrix> ElementMatrices { get; }

		public override int NumColumns { get; }

		public override int NumRows => NumColumns;

		public override void Clear()
		{
			ElementMatrices.Clear();
		}

		public override IMatrix CreateZeroMatrixWithSameFormat()
		{
			var result = new ElementWiseMatrixMonolithic(Elements, DofOrdering);
			return result;
		}

		public override bool HasSameFormat(IReadOnlyMatrix otherMatrix)
		{
			if (otherMatrix is ElementWiseMatrixMonolithic casted)
			{
				if (casted.Elements == Elements && casted.DofOrdering == DofOrdering)
				{
					return true;
					//return DictionariesHaveSameKeys(this.elementMatrices, casted.elementMatrices);
				}
			}

			return false;
		}

		public override void MultiplyIntoResult(IReadOnlyVector lhsVector, IVector rhsVector, bool transposeThis = false)
		{
			if (transposeThis)
			{
				throw new NotImplementedException();
			}

			var x = (Vector)lhsVector;
			var y = (Vector)rhsVector;
			y.Clear();
			foreach (ISuperElement element in Elements)
			{
				int[] elementToDomainDofs = DofOrdering.MapDofsElementToDomain(element);
				Vector xe = x.GetSubvector(elementToDomainDofs);
				var ye = Vector.CreateZero(elementToDomainDofs.Length);
				ElementMatrices[element.ID].MultiplyIntoResult(xe, ye);
				y.AddIntoThisNonContiguouslyFrom(elementToDomainDofs, ye);
			}
		}
	}
}
