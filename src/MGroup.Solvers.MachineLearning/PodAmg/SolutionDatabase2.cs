namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution.LinearSystem;

	public class SolutionDatabase2
	{
		private readonly bool ensureSameLengthVectors;
		private readonly Dictionary<int, double[]> savedModelParameters;
		private readonly Dictionary<int, SortedDictionary<int, Vector>> savedSolutions;

		private int commonVectorLength = -1;

		public SolutionDatabase2(bool ensureSameLengthVectors=true)
		{
			savedModelParameters = new Dictionary<int, double[]>();
			savedSolutions = new Dictionary<int, SortedDictionary<int, Vector>>();
			this.ensureSameLengthVectors = ensureSameLengthVectors;
		}

		public bool CopyParametersArray { get; set; } = true;

		public bool IsEmpty { get; private set; } = true;

		public void CheckSameCountOfParameterSetsAndSolutionVectors()
		{
			if (CountParameterSets() != CountAllSolutions())
			{
				throw new Exception($"Have gathered {CountParameterSets()} sets of model parameters, " +
					$"but {CountAllSolutions()} solution vectors, while using initial preconditioner.");
			}
		}

		public void Clear()
		{
			savedModelParameters.Clear();
			savedSolutions.Clear();
			commonVectorLength = -1;
			IsEmpty = true;
		}

		public int CountParameterSets() => savedModelParameters.Count;

		public int CountAllSolutions()
		{
			int count = 0;
			foreach (var solutionsOfParameterSet in savedSolutions.Values)
			{
				count += solutionsOfParameterSet.Count;
			}
			return count;
		}

		public IEnumerable<int> EnumerateParameterSets() => savedModelParameters.Keys;

		public IEnumerable<Vector> EnumerateSolutionsForParameterSet(int parameterSetId)
			=> savedSolutions[parameterSetId].Values;

		public IEnumerable<Vector> EnumerateAllSolutions() => savedSolutions.Values.SelectMany(dict => dict.Values);

		public double[] GetModelParameters(int parameterSetId)
		{
			return savedModelParameters[parameterSetId];
		}

		public Vector GetSolution(int parameterSetId, int timeStep)
		{
			return savedSolutions[parameterSetId][timeStep];
		}

		public void SaveModelParameters(int parameterSetId, double[] modelParameters)
		{
			double[] savedArray = modelParameters;
			if (CopyParametersArray)
			{
				savedArray = new double[modelParameters.Length];
				Array.Copy(modelParameters, savedArray, modelParameters.Length);
			}
			savedModelParameters[parameterSetId] = savedArray;
		}

		public void SaveSolution(int parameterSetId, int timeStep, Vector solution)
		{
			if (IsEmpty)
			{
				IsEmpty = false;
				commonVectorLength = solution.Length;
			}
			else
			{
				if (solution.Length != commonVectorLength)
				{
					throw new Exception("All solution vectors must have the same length, but the solution vector corresponding" +
						$" to parameter set = {parameterSetId} and time step = {timeStep} has length={solution.Length}, " +
						$"while the previous ones had length={commonVectorLength}");
				}
			}

			SortedDictionary<int, Vector> solutionsOfRealization;
			if (!savedSolutions.TryGetValue(parameterSetId, out solutionsOfRealization))
			{
				solutionsOfRealization = new SortedDictionary<int, Vector>();
				savedSolutions[parameterSetId] = solutionsOfRealization;
			}
			solutionsOfRealization[timeStep] = solution.Copy();
		}
		
	}
}
