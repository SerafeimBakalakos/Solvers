using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;

namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class ConstantPodAmgPreconditioner : IDynamicMLPreconditioner
	{
		private readonly PodAmgPreconditioner singlePreconditioner;

		public ConstantPodAmgPreconditioner(PodAmgPreconditioner preconditioner)
		{
			singlePreconditioner = preconditioner;
		}

		public IPreconditioner CopyWithInitialSettings()
		{
			var singlePreconditionerClone = (PodAmgPreconditioner)singlePreconditioner.CopyWithInitialSettings();
			return new ConstantPodAmgPreconditioner(singlePreconditionerClone);
		}

		public void Initialize(int numDofs, int numPrincipalComponentsInPod, SolutionDatabaseDynamic savedSolutions)
		{
			// Gather all previous solution vectors as columns of a matrix
			var numSamples = savedSolutions.CountAllSolutions();
			var solutionVectors = Matrix.CreateZero(numDofs, numSamples);
			var col = 0;
			foreach (Vector solution in savedSolutions.EnumerateAllSolutions())
			{
				solutionVectors.SetSubcolumn(col, solution);
				col++;
			}

			// AMG-POD training
			singlePreconditioner.Initialize(solutionVectors, numPrincipalComponentsInPod);
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
			=> singlePreconditioner.SolveLinearSystem(rhsVector, lhsVector);

		public void UpdateForTimeStep(int timeStep)
		{
			// Do nothing, since the preconditioner is the same for every timestep
		}

		public void UpdateMatrix(IMatrixView matrix, bool isPatternModified)
			=> singlePreconditioner.UpdateMatrix(matrix, isPatternModified);
	}
}
