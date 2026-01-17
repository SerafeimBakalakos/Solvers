using MGroup.Solvers.DiscretizationExtensions;

namespace MGroup.Solvers.Discretization
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
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearAlgebraExtensions.Views;

	public class DefaultElement : ISuperElement
	{
		protected readonly IModel_v2 model;
		protected readonly ConstrainedDofLocator constrainedDofLocator;
		protected readonly IElementMatrixProvider elementMatrixProvider;

		public DefaultElement(IElementType femElement, IModel_v2 model, ConstrainedDofLocator constrainedDofs,
			IElementMatrixProvider elementMatrixProvider)
		{
			ElementEntity = femElement;
			this.model = model;
			constrainedDofLocator = constrainedDofs;
			this.elementMatrixProvider = elementMatrixProvider;
		}

		public IElementType ElementEntity { get; }

		public int ID => ElementEntity.ID;

		public IMatrix BuildMatrix()
		{
			IMatrix matrix = elementMatrixProvider.Matrix(ElementEntity);
			int[] freeToAllDofs = MapFreeToAllDofs();
			if (freeToAllDofs.Length == 0) 
			{
				return matrix; // No constrained dofs. Use the whole matrix
			}
			else
			{
				return new SubmatrixView(matrix, freeToAllDofs);
			}
		}

		public IEnumerable<INode> EnumerateNodes() => ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);

		public virtual IntDofTable GetDofs()
		{
			IReadOnlyList<INode> elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			var freeDofs = new IntDofTable();
			var freeToAllDofs = new List<int>(elementNodes.Count * elementDofs[0].Count);
			int freeDofIdx = 0;
			for (int nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				INode node = elementNodes[nodeIdx];
				for (int dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					IDofType dofType = elementDofs[nodeIdx][dofIdx];
					if (!constrainedDofLocator.IsConstrainedDof(node, dofType))
					{
						int dofID = model.DofTypes.GetIdOfDof(dofType);
						freeDofs?.TryAdd(node.ID, dofID, freeDofIdx);
						++freeDofIdx;
					}
				}
			}

			return freeDofs;
		}

		/// <summary>
		/// Calculates an int[] map with map.Length = number of free dofs. For each free dof index, the total dof index for the element is stored (counting both free and constrained dofs), which matches the rows & columns of IElementMatrixProvider.Matrix(IElementType). If there are no constrained dofs, then an int[0] is returned instead.
		/// </summary>
		/// <returns>The free-to-all dofs int[] array (or int[0])</returns>
		protected virtual int[] MapFreeToAllDofs()
		{
			IReadOnlyList<INode> elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			var freeToAllDofs = new List<int>(elementNodes.Count * elementDofs[0].Count);
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
						freeToAllDofs.Add(allDofIdx);
					}

					++allDofIdx;
				}
			}

			return hasConstrainedDofs ? freeToAllDofs.ToArray() : Array.Empty<int>();
		}
	}
}
