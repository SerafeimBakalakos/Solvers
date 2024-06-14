namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;

	public class BulkSolutionsTrainingStrategy : ISolutionTrainingStrategy
	{
		private readonly int timeStepSavePeriod;

		public BulkSolutionsTrainingStrategy(int timeStepSavePeriod = 1)
		{
			this.timeStepSavePeriod = timeStepSavePeriod;
		}

		public bool MustSaveSolution(int timeStep) => timeStep % timeStepSavePeriod == 0; // Saved at time steps: 0, T, 2T, 3T

		public bool MustUpdatePreconditioner(int timeStep) => timeStep == 0;
	}
}
