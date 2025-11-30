namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition.FEM
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers;

	public class DefaultElement_temp : ISuperElement
	{
		private readonly ActiveDofs allDofs;

		public DefaultElement_temp(IElementType femElement, ActiveDofs allDofs)
		{
			ElementEntity = femElement;
			this.allDofs = allDofs;
		}

		public IElementType ElementEntity { get; }

		public int ID => ElementEntity.ID;

		public IMatrix BuildMatrix()
		{
			return ((IStructuralElementType)ElementEntity).StiffnessMatrix();
		}

		public IVector BuildRhsVector() => throw new NotImplementedException();

		public IntDofTable GetDofs()
		{
			var elementNodes = ElementEntity.DofEnumerator.GetNodesForMatrixAssembly(ElementEntity);
			var elementDofs = ElementEntity.DofEnumerator.GetDofTypesForMatrixAssembly(ElementEntity);

			var result = new IntDofTable();
			var elementDofIdx = 0;
			for (var nodeIdx = 0; nodeIdx < elementNodes.Count; ++nodeIdx)
			{
				var nodeID = elementNodes[nodeIdx].ID;
				for (var dofIdx = 0; dofIdx < elementDofs[nodeIdx].Count; ++dofIdx)
				{
					var dofID = allDofs.GetIdOfDof(elementDofs[nodeIdx][dofIdx]);
					result.TryAdd(nodeID, dofID, elementDofIdx);
					++elementDofIdx;
				}
			}

			return result;
		}
	}
}
