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

		public void Update()
		{
			elementScalingMatrices = new Dictionary<int, DiagonalMatrix>();
			foreach (ISuperElement element in elements)
			{
				// Multiplicities of element dofs
				IntDofTable elementDofs = element.GetDofs();
				int numDofs = elementDofs.NumEntries;
				var dofMultiplicities = new int[numDofs];
				foreach (INode node in element.EnumerateNodes())
				{
					int nodeMultiplicity = partition.FindMultiplicityOfNode(node.ID);
					foreach (int dofIdx in elementDofs.GetValuesOfRow(node.ID))
					{
						dofMultiplicities[dofIdx] = nodeMultiplicity;
					}
				}

				// Invert
				var inverseMultiplicities = new double[numDofs];
				for (int i = 0; i < numDofs; i++)
				{
					inverseMultiplicities[i] = 1.0 / dofMultiplicities[i];
				}

				elementScalingMatrices[element.ID] = DiagonalMatrix.CreateFromArray(inverseMultiplicities);
			}
		}

		public DiagonalMatrix GetScalingMatrix(int elementID) => elementScalingMatrices[elementID];
	}
}
