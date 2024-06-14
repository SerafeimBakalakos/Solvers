namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public interface ISolutionTrainingStrategy
	{
		public bool MustSaveSolution(int timeStep);

		public bool MustUpdatePreconditioner(int timeStep);
	}
}
