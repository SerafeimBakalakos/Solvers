namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;

	public interface IDynamicMLPreconditioner : IPreconditioner
	{
		//TODO: Perhaps the arguments should be injected into the constructor.
		//		As it is, the interface is only for POD-based preconditioners.
		public void Initialize(int numDofs, int numPrincipalComponentsInPod, SolutionDatabaseDynamic savedSolutions);

		public void UpdateForTimeStep(int timeStep);
	}
}
