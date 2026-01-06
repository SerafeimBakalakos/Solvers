namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers;
	using MGroup.Solvers.DofOrdering;

	public class DefaultElementCaching_v1 : ISuperElement
	{
		private readonly IModel_v2 model;
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;

		private int[] freeToAllDofs;

		public DefaultElementCaching_v1(IElementType femElement, IModel_v2 model, ConstrainedDofLocator constrainedDofs,
			IElementMatrixProvider elementMatrixProvider)
		{
			ElementEntity = femElement;
			this.model = model;
			this.constrainedDofLocator = constrainedDofs;
			this.elementMatrixProvider = elementMatrixProvider;
		}

		public IElementType ElementEntity { get; }

		public int ID => ElementEntity.ID;

		public IMatrix BuildMatrix()
		{
			IMatrix matrix = elementMatrixProvider.Matrix(ElementEntity);
			if (freeToAllDofs == null)
			{
				return matrix;
			}
			else
			{
				return matrix.GetSubmatrix(freeToAllDofs, freeToAllDofs);
			}
		}

		public IVector BuildRhsVector_temp() => throw new NotImplementedException();

		public IEnumerable<INode> EnumerateNodes() => ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);

		public IntDofTable GetDofs()
		{
			IReadOnlyList<INode> elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			var freeDofs = new IntDofTable();
			var freeToAllDofs = new List<int>(elementNodes.Count * elementDofs[0].Count);
			int freeDofIdx = 0;
			int allDofIdx = 0;
			bool hasConstrainedDofs = false;
			for (int nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				INode node = elementNodes[nodeIdx];
				for (int dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					IDofType dofType = elementDofs[nodeIdx][dofIdx];
					if (constrainedDofLocator.IsConstrainedDof(node, dofType))
					{
						hasConstrainedDofs = true;
					}
					else
					{
						int dofID = model.DofTypes.GetIdOfDof(dofType);
						freeDofs?.TryAdd(node.ID, dofID, freeDofIdx);
						freeToAllDofs.Add(allDofIdx);
						++freeDofIdx;
					}

					++allDofIdx;
				}
			}

			if (hasConstrainedDofs)
			{
				this.freeToAllDofs = freeToAllDofs.ToArray(); // It will be used for matrix assembly
			}

			return freeDofs;
		}
	}
}
