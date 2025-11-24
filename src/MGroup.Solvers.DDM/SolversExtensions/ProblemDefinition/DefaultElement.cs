namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	public class DefaultElement //: ISuperElement
	{
		private readonly ActiveDofs allDofs;

		public DefaultElement(IElementType femElement, ActiveDofs allDofs)
		{
			ElementEntity = femElement;
			this.allDofs = allDofs;
		}

		public IElementType ElementEntity { get; }

		public IntDofTable GetDofs()
		{
			IReadOnlyList<INode> elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			var result = new IntDofTable();
			int elementDofIdx = 0;
			for (int nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				int nodeID = elementNodes[nodeIdx].ID;
				for (int dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					int dofID = allDofs.GetIdOfDof(elementDofs[nodeIdx][dofIdx]);
					result.TryAdd(nodeID, dofID, elementDofIdx);
					++elementDofIdx;
				}
			}

			return result;
		}
	}
}
