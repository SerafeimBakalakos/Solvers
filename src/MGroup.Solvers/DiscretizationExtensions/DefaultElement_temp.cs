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

	public class DefaultElement_temp : ISuperElement
	{
		private readonly IModel_v2 model;
		private readonly ConstrainedDofLocator constrainedDofLocator;
		private readonly IElementMatrixProvider elementMatrixProvider;

		private IntDofTable freeDofs;
		private int[] freeToAllDofs;
		public DefaultElement_temp(IElementType femElement, IModel_v2 model, ConstrainedDofLocator constrainedDofs,
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
			return matrix.GetSubmatrix(freeToAllDofs, freeToAllDofs);
		}

		public IVector BuildRhsVector_temp() => throw new NotImplementedException();

		public IntDofTable GetDofs() => freeDofs;

		public IEnumerable<INode> EnumerateNodes() => ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);

		public void PrepareDofs()
		{
			#region debug
			if (freeDofs != null)
			{
				throw new Exception("This must happen only once for now (linear-static analysis)");
			}
			#endregion

			IReadOnlyList<INode> elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			freeDofs = new IntDofTable();
			var freeToAllDofs = new List<int>(elementNodes.Count * elementDofs[0].Count);
			int freeDofIdx = 0;
			int allDofIdx = 0;
			for (int nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				INode node = elementNodes[nodeIdx];
				for (int dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					IDofType dofType = elementDofs[nodeIdx][dofIdx];
					if (!constrainedDofLocator.IsConstrainedDof(node, dofType))
					{
						int dofID = model.DofTypes.GetIdOfDof(dofType);
						freeDofs.TryAdd(node.ID, dofID, freeDofIdx);
						freeToAllDofs.Add(allDofIdx);
						++freeDofIdx;
					}
					++allDofIdx;
				}
			}

			this.freeToAllDofs = freeToAllDofs.ToArray();
		}
	}
}
