namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;

	public interface ISolutionTrainingStrategy
	{
		public IDynamicMLPreconditioner CreatePreconditioner(PodAmgPreconditioner examplePreconditioner);

		public bool MustSaveSolution(int timeStep);
	}
}
