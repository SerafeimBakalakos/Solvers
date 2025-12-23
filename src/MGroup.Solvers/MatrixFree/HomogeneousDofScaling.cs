namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Numerics;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;

	public class HomogeneousDofScaling : IDofScaling
	{
		private readonly Dictionary<int, DiagonalMatrix> elementScalingMatrices = new Dictionary<int, DiagonalMatrix>();
		private readonly IElementPartition partition;

		public HomogeneousDofScaling(IElementPartition partition)
		{
			this.partition = partition;
		}

		public void Calculate(PartitionedMatrixGlobal partitionedMatrix)
		{
			elementScalingMatrices.Clear();
			ISubdomainDofOrdering_v2 dofOrdering = partitionedMatrix.DofOrdering;
			foreach (ISuperElement element in partitionedMatrix.Elements)
			{
				// Multiplicities of all element dofs
				IntDofTable elementDofs = element.GetDofs();
				var dofMultiplicitiesAll = new int[elementDofs.NumEntries];
				foreach (INode node in element.EnumerateNodes())
				{
					foreach (int dofIdx in elementDofs.GetValuesOfRow(node.ID))
					{
						dofMultiplicitiesAll[dofIdx] = partition.FindMultiplicityOfNode(node.ID);
					}
				}

				// Multiplicities of active dofs only (e.g. free dofs)
				(int[] elementDofIndices, int[] subdomainDofIndices) = dofOrdering.MapDofsElementToSubdomain(element);
				int numActiveDofs = elementDofIndices.Length;
				var dofMultiplicitiesActive = new int[numActiveDofs];
				for (int i = 0; i < numActiveDofs; i++)
				{
					dofMultiplicitiesActive[i] = dofMultiplicitiesAll[elementDofIndices[i]];
				}

				// Invert
				var inverseMultiplicities = new double[numActiveDofs];
				for (int i = 0; i < numActiveDofs; i++)
				{
					inverseMultiplicities[i] = 1.0 / dofMultiplicitiesAll[elementDofIndices[i]];
				}

				elementScalingMatrices[element.ID] = DiagonalMatrix.CreateFromArray(inverseMultiplicities);
			}
		}

		public DiagonalMatrix GetScalingMatrix(int elementID) => elementScalingMatrices[elementID];
	}
}
