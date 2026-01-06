namespace MGroup.Solvers.MatrixFree.Dofs
{
	using System;
	using System.Collections.Generic;
	using System.Numerics;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;

	public class HomogeneousDofScaling : IDofScaling
	{
		private readonly ISubdomain_v2 domain;
		private readonly IComputeEnvironment environment;
		private readonly IElementPartition partition;
		
		private Dictionary<int, DiagonalMatrix> elementScalingMatrices;

		public HomogeneousDofScaling(IComputeEnvironment environment, ISubdomain_v2 domain, IElementPartition partition)
		{
			this.environment = environment;
			this.domain = domain;
			this.partition = partition;
		}

		public DiagonalMatrix GetScalingMatrix(int elementID) => elementScalingMatrices[elementID];

		public void Initialize()
		{
			elementScalingMatrices = environment.CalcNodeData(elementID =>
			{
				ISuperElement element = domain.GetElement(elementID);
				IntDofTable elementDofs = element.GetDofs();
				int numElementDofs = elementDofs.NumEntries;

				// Multiplicities of element dofs
				var dofMultiplicities = new int[numElementDofs];
				foreach (INode node in element.EnumerateNodes())
				{
					int nodeMultiplicity = partition.FindMultiplicityOfNode(node.ID);
					foreach (int dofIdx in elementDofs.GetValuesOfRow(node.ID))
					{
						dofMultiplicities[dofIdx] = nodeMultiplicity;
					}
				}

				// Invert
				var inverseMultiplicities = new double[numElementDofs];
				for (int i = 0; i < numElementDofs; i++)
				{
					inverseMultiplicities[i] = 1.0 / dofMultiplicities[i];
				}

				return DiagonalMatrix.CreateFromArray(inverseMultiplicities);
			});
		}
	}
}
