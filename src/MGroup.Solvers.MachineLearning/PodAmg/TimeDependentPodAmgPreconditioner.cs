using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;

namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class TimeDependentPodAmgPreconditioner : IDynamicMLPreconditioner
	{
		private readonly PodAmgPreconditioner[] preconditioners; // one per time-step

		private bool currentIsPatternModified;
		private IMatrixView currentSystemMatrix;
		private int currentTimeStep;

		public TimeDependentPodAmgPreconditioner(int numTimeSteps, PodAmgPreconditioner examplePreconditioner)
		{
			preconditioners = new PodAmgPreconditioner[numTimeSteps];
			for (var t = 0; t < numTimeSteps; t++)
			{
				preconditioners[t] = (PodAmgPreconditioner)examplePreconditioner.CopyWithInitialSettings();
			}
		}

		public IPreconditioner CopyWithInitialSettings()
		{
			var singlePreconditionerClone = (PodAmgPreconditioner)preconditioners[0].CopyWithInitialSettings();
			return new TimeDependentPodAmgPreconditioner(preconditioners.Length, singlePreconditionerClone);
		}

		public void Initialize(int numDofs, int numPrincipalComponentsInPod, SolutionDatabase2 savedSolutions)
		{
			var numTimeSteps = preconditioners.Length;
			var numSamples = savedSolutions.CountParameterSets();
			for (var t = 0; t < numTimeSteps; t++)
			{
				// Gather the previous solution vectors corresponding to this time-step. Put them in a matrix as columns.
				var solutionVectors = Matrix.CreateZero(numDofs, numSamples);
				var col = 0;
				foreach (var parameterSet in savedSolutions.EnumerateParameterSets())
				{
					var solution = savedSolutions.GetSolution(parameterSet, t);
					solutionVectors.SetSubcolumn(col, solution);
					col++;
				}

				// AMG-POD training
				preconditioners[t].Initialize(solutionVectors, numPrincipalComponentsInPod);
			}
		}

		public void SolveLinearSystem(IVectorView rhsVector, IVector lhsVector)
			=> preconditioners[currentTimeStep].SolveLinearSystem(rhsVector, lhsVector);

		public void UpdateForTimeStep(int timeStep)
		{
			currentTimeStep = timeStep;
			preconditioners[timeStep].UpdateMatrix(currentSystemMatrix, currentIsPatternModified);
		}

		public void UpdateMatrix(IMatrixView matrix, bool isPatternModified)
		{
			currentSystemMatrix = matrix;
			currentIsPatternModified = isPatternModified;
			currentTimeStep = -1;
		}
	}
}
