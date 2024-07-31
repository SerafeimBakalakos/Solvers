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
		private readonly int timeStepSavePeriod;

		public ConstantPodAmgPreconditioner(PodAmgPreconditioner preconditioner, int timeStepSavePeriod = 1)
		{
			singlePreconditioner = preconditioner;
			this.timeStepSavePeriod = timeStepSavePeriod;
		}

		public IPreconditioner CopyWithInitialSettings()
		{
			var singlePreconditionerClone = (PodAmgPreconditioner)singlePreconditioner.CopyWithInitialSettings();
			return new ConstantPodAmgPreconditioner(singlePreconditionerClone, timeStepSavePeriod);
		}

		public void Initialize(int numDofs, int numPrincipalComponentsInPod, SolutionDatabaseDynamic savedSolutions)
		{
			// Gather the required previous solution vectors as columns of a matrix
			Matrix solutionVectors = savedSolutions.ToMatrixSolutionsAsColumns(true, t => t % timeStepSavePeriod == 0);

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
