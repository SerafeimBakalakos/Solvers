namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;

	public class SeparateTimeStepSolutionsTrainingStrategy : ISolutionTrainingStrategy
	{
		private readonly int numTimeSteps;

		public SeparateTimeStepSolutionsTrainingStrategy(int numTimeSteps)
		{
			this.numTimeSteps = numTimeSteps;
		}

		public IDynamicMLPreconditioner CreatePreconditioner(PodAmgPreconditioner examplePreconditioner)
			=> new TimeDependentPodAmgPreconditioner(numTimeSteps, examplePreconditioner);

		public bool MustSaveSolution(int timeStep) => true;

	}
}
