namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using DotNumerics.ODE.Radau5;

	using Google.Protobuf.WellKnownTypes;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Solution.LinearSystem;

	public class SolutionDatabaseDynamic
	{
		private const int UnknownNumDofs = -1;

		private readonly Dictionary<int, double[]> savedModelParameters;
		private readonly List<int> savedParameterSetIDs;

		//TODO: This can be saved in a 1D list and the indices inferred by the param set IDs and timesteps
		private readonly Dictionary<int, Dictionary<int, Vector>> savedSolutions; 

		/// <summary>
		/// Must be the same for all saved parameter sets. Determined by all saved vectors of the first parameter set.
		/// </summary>
		private readonly List<int> savedTimeSteps;


		/// <summary>
		/// Must be the same for all saved solution vectors (equal to their length). Determined by the first one.
		/// </summary>
		private int numDofs = UnknownNumDofs;

		public SolutionDatabaseDynamic()
		{
			savedModelParameters = new Dictionary<int, double[]>();
			savedParameterSetIDs = new List<int>();
			savedSolutions = new Dictionary<int, Dictionary<int, Vector>>();
			savedTimeSteps = new List<int>();
		}

		public bool CopyParametersArray { get; set; } = true;

		public void Clear()
		{
			savedModelParameters.Clear();
			savedParameterSetIDs.Clear();
			savedSolutions.Clear();
			savedTimeSteps.Clear();
			numDofs = UnknownNumDofs;
		}

		public int CountDofs() => numDofs;

		public int CountAllSolutions()
		{
			//int count = 0;
			//foreach (var solutionsOfParameterSet in savedSolutions.Values)
			//{
			//	count += solutionsOfParameterSet.Count;
			//}
			//return count;

			return savedTimeSteps.Count * savedParameterSetIDs.Count;
		}

		public int CountModelParameters()
		{
			int numParams = savedModelParameters.First().Value.Length;
#if DEBUG
			foreach (var paramsOfParameterSet in savedModelParameters.Values)
			{
				if (paramsOfParameterSet.Length != numParams)
				{
					throw new Exception("Different number of parameters for different parameter sets.");
				}
			}
#endif
			return numParams;
		}

		public int CountParameterSets() => savedParameterSetIDs.Count;

		public int CountTimeSteps()
		{
			int numTimeSteps = savedTimeSteps.Count;
#if DEBUG
			foreach (var solutionsOfParameterSet in savedSolutions.Values)
			{
				if (solutionsOfParameterSet.Count != numTimeSteps)
				{
					throw new Exception("Different number of time steps for different parameter sets.");
				}
			}
#endif
			return numTimeSteps;
		}

		public IEnumerable<int> EnumerateParameterSetIDs() => savedParameterSetIDs;

		public IEnumerable<int> EnumerateTimeSteps() => savedTimeSteps;

		/// <summary>
		/// Returns all saved solution vectors. The order depends on <paramref name="consecutiveTimeSteps"/>.
		/// </summary>
		/// <param name="consecutiveTimeSteps">
		/// If true, the solution vectors corresponding to all timesteps of the same model parameter set will be consecutive.
		/// If false, the solution vectors corresponding to all parameter sets of the same timestep will be consecutive.
		/// </param>
		/// <returns></returns>
		public IEnumerable<Vector> EnumerateAllSolutions(bool consecutiveTimeSteps)
		{
			if (consecutiveTimeSteps)
			{
				foreach (int paramSetId in savedParameterSetIDs)
				{
					var solutionsOfParamSet = savedSolutions[paramSetId];
					foreach (int timestep in savedTimeSteps)
					{
						yield return solutionsOfParamSet[timestep];
					}
				}
			}
			else
			{
				foreach (int timestep in savedTimeSteps)
				{
					foreach (int paramSetId in savedParameterSetIDs)
					{
						yield return savedSolutions[paramSetId][timestep];
					}
				}
			}
		}

		public IEnumerable<Vector> EnumerateSolutionsForTimestep(int timestep)
		{
			if (!(savedSolutions[savedParameterSetIDs[0]].ContainsKey(timestep)))
			{
				throw new Exception($"No solution vectors are saved for the requested timestep = {timestep}.");
			}

			foreach (int parameterSet in savedParameterSetIDs)
			{
				yield return savedSolutions[parameterSet][timestep];
			}
		}

		public IEnumerable<Vector> EnumerateSolutionsForParameterSet(int parameterSetID)
		{
			if (!(savedSolutions.ContainsKey(parameterSetID)))
			{
				throw new Exception($"No solution vectors are saved for the requested parameter set = {parameterSetID}.");
			}

			var solutionsOfParamSet = savedSolutions[parameterSetID];
			foreach (int t in savedTimeSteps)
			{
				yield return solutionsOfParamSet[t];
			}
		}

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
			double[] savedArray;
			if (CopyParametersArray)
			{
				savedArray = new double[modelParameters.Length];
				Array.Copy(modelParameters, savedArray, modelParameters.Length);
			}
			else
			{
				savedArray = modelParameters;
			}

			bool didNotExist = savedModelParameters.TryAdd(parameterSetId, savedArray);
			if (didNotExist)
			{
				savedParameterSetIDs.Add(parameterSetId);
				savedSolutions.Add(parameterSetId, new Dictionary<int, Vector>());
			}
			else
			{
				throw new Exception($"Trying to save the parameter set = {parameterSetId} failed," +
					$" since it was already saved previously");
			}
		}

		public void SaveSolution(int parameterSetId, int timeStep, Vector solution)
		{
			// Check dofs
			if (numDofs == UnknownNumDofs)
			{
				numDofs = solution.Length;
			}
			else
			{
				if (solution.Length != numDofs)
				{
					throw new Exception($"The solution vector corresponding to parameter set = {parameterSetId} and" +
						$" time step = {timeStep} has length = {solution.Length}, while the previous ones had length={numDofs}");
				}
			}

			// Check parameter set
			bool paramSetExists = savedSolutions.TryGetValue(parameterSetId, out Dictionary<int, Vector> solutionsOfParam);
			if (!paramSetExists)
			{
				throw new Exception($"The parameter set = {parameterSetId} is not saved in the database.");
			}

			// Save the solution vector
			bool timeStepDoesNotExist = solutionsOfParam.TryAdd(timeStep, solution.Copy());

			// Check timestep
			if (timeStepDoesNotExist)
			{
				if (savedParameterSetIDs.Count == 1) // Determine the unique time steps.
				{
					// This timestep is unique. If it weren't, an exception would have been thrown, when saving it.
					savedTimeSteps.Add(timeStep);
				}
				else // Check that this timestep is one of those saved for previous parameter sets
				{
					Dictionary<int, Vector> solutionsOfFirstParamSet = savedSolutions[savedParameterSetIDs[0]];
					if (!solutionsOfFirstParamSet.ContainsKey(timeStep))
					{
						throw new Exception($"Cannot save time step = {timeStep} for parameter set = {parameterSetId}," +
							$" since it was not saved for previous parameter sets");
					}
				}
			}
			else
			{
				throw new Exception($"Time step = {timeStep} was already saved for parameter set = {parameterSetId}");
			}
		}

		public double[,,] ToArray3DAllSolutions()
		{
			int numDofs = this.numDofs;
			int numParameterSets = CountParameterSets();
			int numTimeSteps = CountTimeSteps();
			var result = new double[numParameterSets, numTimeSteps, numDofs];
			int p = 0;
			foreach (int paramSet in EnumerateParameterSetIDs())
			{
				int t = 0;
				foreach (int timeStep in EnumerateTimeSteps())
				{
					double[] solution = GetSolution(paramSet, timeStep).RawData;
					SetArrayAlongDim2(result, p, t, solution);
					t++;
				}
				p++;
			}
			return result;
		}

		
		public double[,] ToArray2DAllSolutionsAsRows(bool consecutiveTimeSteps)
		{
			int numDofs = this.numDofs;
			int numVectorsTotal = CountAllSolutions();
			var result = new double[numVectorsTotal, numDofs];
			int row = 0;
			foreach (Vector solution in EnumerateAllSolutions(consecutiveTimeSteps))
			{
				SetRow(result, row, solution.RawData);
				row++;
			}
			return result;
		}

		/// <summary>
		/// Each row of the returned matrix is an array that stores the timestep, followed by the model parameters: 
		/// [timestep, param0, param1, ..., paramN]. The rows are in the same order as those in the matrix returned by 
		/// <see cref="ToArray2DAllSolutionsAsRows(bool)"/>, provided that the same value for the flag
		/// <paramref name="consecutiveTimeSteps"/> is passed in.
		/// </summary>
		/// <param name="consecutiveTimeSteps">
		/// If true, the rows of consecutive timesteps, but identical model parameters will be consecutive. E.g.
		/// {{t0, p00, p10, p20, ... pN0}, {t1, p00, p10, p20, ... pN0}, ... {tN, p00, p10, p20, ... pN0}, 
		///  {t0, p01, p11, p21, ... pN1}, {t1, p01, p11, p21, ... pN1}, ... }.
		/// If false, the rows corresponding to all parameter sets of the same timestep will be listed, then the same for the 
		/// next timestep, etc. E.g.
		/// {{t0, p00, p10, p20, ... pN0}, {t0, p01, p11, p21, ... pN1}, ... {t0, p0M, p1M, p2M, ... pNM}.
		///  {t1, p00, p10, p20, ... pN0}, {t1, p01, p11, p21, ... pN1}, ...}.
		/// </param>
		/// <returns></returns>
		public double[,] ToArray2DAllParametersAndTimestepsAsRows(bool consecutiveTimeSteps)
		{
			int numParams = 1 + CountModelParameters(); // The first parameter will be the timestep
			int numVectorsTotal = CountAllSolutions(); // This is numParameterSets * numTim
			var result = new double[numVectorsTotal, numParams];
			int row = 0;
			if (consecutiveTimeSteps)
			{
				foreach (int paramSetID in EnumerateParameterSetIDs())
				{
					double[] parameters = GetModelParameters(paramSetID);
					foreach (int timeStep in EnumerateTimeSteps())
					{
						double[] paramsAndTimestep = Prepend(timeStep, parameters);
						SetRow(result, row, paramsAndTimestep);
						row++;
					}
				}
			}
			else
			{
				foreach (int timeStep in EnumerateTimeSteps())
				{
					foreach (int paramSetID in EnumerateParameterSetIDs())
					{
						double[] parameters = GetModelParameters(paramSetID);
						double[] paramsAndTimestep = Prepend(timeStep, parameters);
						SetRow(result, row, paramsAndTimestep);
						row++;
					}
				}
			}

			return result;
		}

		public Matrix ToMatrixAllSolutionsAsColumns(bool consecutiveTimeSteps)
		{
			int numDofs = this.numDofs;
			int numVectorsTotal = CountAllSolutions();
			var result = Matrix.CreateZero(numDofs, numVectorsTotal);
			int col = 0;
			foreach (Vector solution in EnumerateAllSolutions(consecutiveTimeSteps))
			{
				result.SetSubcolumn(col, solution);
				col++;
			}
			return result;
		}

		public Matrix ToMatrixSolutionsAsColumnsForTimestep(int timestep)
		{
			int numDofs = this.numDofs;
			int numParamSets = CountParameterSets();
			var result = Matrix.CreateZero(numDofs, numParamSets);
			int col = 0;
			foreach (Vector solution in EnumerateSolutionsForTimestep(timestep)) 
			{
				result.SetSubcolumn(col, solution);
				col++;
			}
			
			return result;
		}

		private double[] Prepend(double newValue, double[] oldArray)
		{
			var result = new double[oldArray.Length + 1];
			result[0] = newValue;
			Array.Copy(oldArray, 0, result, 1, oldArray.Length);
			return result;
		}

		private void SetRow(double[,] array2D, int rowIdx, double[] rowValues)
		{
			int numCols = array2D.GetLength(1);
			Debug.Assert(rowValues.Length == numCols);
			//Array.Copy(rowValues, 0, array2D, rowIdx * numCols, numCols);
			int size = sizeof(double);
			System.Buffer.BlockCopy(rowValues, 0, array2D, size * rowIdx * numCols, size * numCols);
		}

		private void SetArrayAlongDim2(double[,,] array3D, int idxDim0, int idxDim1, double[] values)
		{
			int dim1Count = array3D.GetLength(1);
			int dim2Count = array3D.GetLength(2);
			Debug.Assert(values.Length == dim2Count);
			int offset = idxDim0 * dim1Count * dim2Count + idxDim1 * dim2Count;
			//Array.Copy(values, 0, array3D, offset, dim2Count);
			int size = sizeof(double);
			System.Buffer.BlockCopy(values, 0, array3D, size * offset, size * dim2Count);
		}
	}
}
