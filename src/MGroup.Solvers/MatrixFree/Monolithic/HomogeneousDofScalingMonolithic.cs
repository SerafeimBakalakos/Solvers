namespace MGroup.Solvers.MatrixFree.Monolithic
{
	using System;
	using System.Collections.Generic;
	using System.Numerics;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.MatrixFree.Dofs;

	public class HomogeneousDofScalingMonolithic : IDofScaling
	{
		private readonly ISubdomainDofOrdering_v2 dofOrdering;
		private readonly IReadOnlyCollection<ISuperElement> elements;
		private readonly IElementPartition partition;

		private Dictionary<int, DiagonalMatrix> elementScalingMatrices;

		public HomogeneousDofScalingMonolithic(IElementPartition partition, IReadOnlyCollection<ISuperElement> elements, ISubdomainDofOrdering_v2 dofOrdering)
		{
			this.partition = partition;
			this.dofOrdering = dofOrdering;
			this.elements = elements;
		}

		public void Initialize()
		{
			elementScalingMatrices = new Dictionary<int, DiagonalMatrix>();
			foreach (ISuperElement element in elements)
			{
				// Multiplicities of all element dofs
				IntDofTable elementDofs = element.GetDofs();
				var dofMultiplicitiesAll = new int[elementDofs.NumEntries];
				foreach (INode node in element.EnumerateNodes())
				{
					int nodeMultiplicity = partition.FindMultiplicityOfNode(node.ID);
					foreach (int dofIdx in elementDofs.GetValuesOfRow(node.ID))
					{
						dofMultiplicitiesAll[dofIdx] = nodeMultiplicity;
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
					inverseMultiplicities[i] = 1.0 / dofMultiplicitiesActive[i];
				}

				elementScalingMatrices[element.ID] = DiagonalMatrix.CreateFromArray(inverseMultiplicities);
			}
		}

		public DiagonalMatrix GetScalingMatrix(int elementID) => elementScalingMatrices[elementID];
	}
}
