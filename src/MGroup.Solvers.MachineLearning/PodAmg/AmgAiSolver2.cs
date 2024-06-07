namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using MGroup.LinearAlgebra.AlgebraicMultiGrid.PodAmg;
	using MGroup.LinearAlgebra.AlgebraicMultiGrid;
	using MGroup.LinearAlgebra.Iterative.Preconditioning;
	using MGroup.LinearAlgebra.Iterative.Stationary.CSR;
	using MGroup.LinearAlgebra.Iterative.Termination.Iterations;
	using MGroup.LinearAlgebra.Iterative;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MachineLearning.TensorFlow;
	using MGroup.MSolve.DataStructures;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.MSolve.Solution;
	using MGroup.Solvers.AlgebraicModel;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Logging;
	using MGroup.LinearAlgebra.Triangulation;
	using MGroup.Solvers.Direct;
	using Serilog.Core;
	using System.Diagnostics;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;

	public class AmgAiSolver2 : ISolver
	{
		private const string name = "POD-AMG solver"; // for error messages

		private readonly IDofOrderer dofOrderer;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IPreconditioner initialPreconditioner;
		private readonly PodAmgPreconditioner amgPreconditioner;
		private readonly bool matrixPatternWillNotBeModified;
		private readonly int numParameterSetsForPod;
		private readonly int numPrincipalComponentsInPod;

		private double[] modelParametersCurrent;
		//private CaeFffnSurrogate surrogate;
		private bool useAmgPreconditioner;

		private int currentParameterSetId;
		private int currentTimeStep;

		private AmgAiSolver2(IDofOrderer dofOrderer, PcgAlgorithm pcgAlgorithm, bool matrixPatternWillNotBeModified,
			IPreconditioner initialPreconditioner, PodAmgPreconditioner amgPreconditioner,
			int numParameterSetsForPod, int numPrincipalComponentsInPod/*, CaeFffnSurrogate surrogate*/)
		{
			this.dofOrderer = dofOrderer;
			this.pcgAlgorithm = pcgAlgorithm;
			this.matrixPatternWillNotBeModified = matrixPatternWillNotBeModified;
			this.initialPreconditioner = initialPreconditioner;
			this.amgPreconditioner = amgPreconditioner;
			this.numParameterSetsForPod = numParameterSetsForPod;
			this.numPrincipalComponentsInPod = numPrincipalComponentsInPod;
			//this.surrogate = surrogate;

			useAmgPreconditioner = false;
			Logger = new SolverLogger(name);
		}

		public GlobalAlgebraicModel<CsrMatrix> AlgebraicModel { get; private set; }

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; private set; }

		public ISolverLogger Logger { get; }

		public string Name => name;

		public SolutionDatabase2 SavedSolutions { get; } = new SolutionDatabase2();

		public void HandleMatrixWillBeSet() { }

		public void Initialize() { }

		public Matrix InverseSystemMatrixTimesOtherMatrix(IMatrixView otherMatrix) => throw new NotImplementedException();

		public void PreventFromOverwrittingSystemMatrices()
		{
			// No factorization is done.
		}

		public void SetModel(int parameterSetId, double[] modelParameters, IModel model)
		{
			currentParameterSetId = parameterSetId;
			currentTimeStep = 0;
			modelParametersCurrent = modelParameters.Copy();
			SavedSolutions.SaveModelParameters(parameterSetId, modelParameters);

			AlgebraicModel = new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, new CsrMatrixAssembler(true));
			this.LinearSystem = AlgebraicModel.LinearSystem;
			this.LinearSystem.Observers.Add(this);
		}

		public void Solve()
		{
			if (useAmgPreconditioner)
			{
				SolveUsingPodAmgPreconditioner();
			}
			else
			{
				if (SavedSolutions.CountParameterSets() < numParameterSetsForPod)
				{
					Vector solution = SolveUsingInitialPreconditioner();
					if (!SavedSolutions.IsEmpty)
					{
						if (solution.Length != SavedSolutions.VectorLength)
						{
							throw new Exception("All solution vectors must have the same length, but the " +
								$"{SavedSolutions.CountAllSolutions() + 1}th solution vector has length={solution.Length}, " +
								$"while the previous ones had length={SavedSolutions.VectorLength}");
						}
					}
					SavedSolutions.SaveSolution(currentParameterSetId, currentTimeStep, LinearSystem.Solution.SingleVector);
				}
				else
				{
					useAmgPreconditioner = true;
					TrainBasedOnFirstSolutions();
					SolveUsingPodAmgPreconditioner();
				}
			}

			++currentTimeStep;
		}

		private void TrainBasedOnFirstSolutions()
		{
			// Gather all previous solution vectors as columns of a matrix
			int numSamples = SavedSolutions.CountAllSolutions();
			int numDofs = AlgebraicModel.LinearSystem.Solution.Length;
			Matrix solutionVectors = Matrix.CreateZero(numDofs, numSamples);
			int col = 0;
			foreach (Vector solution in SavedSolutions.EnumerateAllSolutions())
			{
				solutionVectors.SetSubcolumn(col, solution);
			}

			// AMG-POD training
			amgPreconditioner.Initialize(solutionVectors, numPrincipalComponentsInPod);

			//// CAE-FFNN training: Gather all previous model parameters
			//if (PreviousModelParameters.Count != numSamples)
			//{
			//	throw new Exception($"Have gathered {PreviousModelParameters.Count} sets of model parameters, " +
			//		$"but {numSamples} solution vectors, while using initial preconditioner.");
			//}

			//int numParameters = modelParametersCurrent.Length;
			//var parametersAsArray = new double[numSamples, numParameters];
			//for (int i = 0; i < numSamples; ++i)
			//{
			//	if (PreviousModelParameters[i].Length != numParameters)
			//	{
			//		throw new Exception("The model parameter sets do not all have the same size");
			//	}

			//	for (int j = 0; j < numParameters; ++j)
			//	{
			//		parametersAsArray[i, j] = PreviousModelParameters[i][j];
			//	}
			//}

			//// CAE-FFNN training:  Dimension 0 must be the number of samples.
			//double[,] solutionsAsArray = solutionVectors.Transpose().CopytoArray2D();
			//surrogate.TrainAndEvaluate(parametersAsArray, solutionsAsArray, null);

			// Free up some memory by deleting the stored solution vectors
			SavedSolutions.Clear();
		}

		private Vector SolveUsingInitialPreconditioner()
		{
			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;

			// Preconditioning
			initialPreconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);

			// Iterative algorithm
			IterativeStatistics stats = pcgAlgorithm.Solve(matrix, initialPreconditioner,
				LinearSystem.RhsVector.SingleVector, LinearSystem.Solution.SingleVector,
				true, () => Vector.CreateZero(systemSize));
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm with "
					+ $"diagonal preconditioner run for {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}

			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
			return LinearSystem.Solution.SingleVector.Copy();
		}

		private void SolveUsingPodAmgPreconditioner()
		{
			CsrMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;
			Vector rhs = LinearSystem.RhsVector.SingleVector;

			//TODO: Delete these when adding the ML prediction
			if (LinearSystem.Solution.SingleVector == null)
			{
				LinearSystem.Solution.SingleVector = Vector.CreateZero(systemSize);
			}
			else
			{
				LinearSystem.Solution.Clear();
			}
			//// Use ML prediction as initial guess.
			//double[] parameters = modelParametersCurrent.Copy();
			//double[] prediction = surrogate.Predict(parameters);
			//var solution = Vector.CreateFromArray(prediction);
			//LinearSystem.Solution.SingleVector = solution;

			amgPreconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);

			IterativeStatistics stats = pcgAlgorithm.Solve(matrix, amgPreconditioner, rhs, LinearSystem.Solution.SingleVector,
				false, () => Vector.CreateZero(systemSize));
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm with "
					+ $"AMG-POD preconditioner run for {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);
		}

		public class Factory
		{
			private readonly int numParameterSetsForPod;
			private readonly int numPrincipalComponentsInPod;
			//private readonly CaeFffnSurrogate.Builder surrogateBuilder;

			public Factory(int numParameterSetsForPod, int numPrincipalComponentsInPod/*, CaeFffnSurrogate.Builder surrogateBuilder*/)
			{
				this.numParameterSetsForPod = numParameterSetsForPod;
				this.numPrincipalComponentsInPod = numPrincipalComponentsInPod;
				//this.surrogateBuilder = surrogateBuilder;
			}

			public IDofOrderer DofOrderer { get; set; }
				= new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

			public bool MatrixPatternWillNotBeModified { get; set; } = false;

			public double PcgConvergenceTolerance { get; set; } = 1E-5;

			public IMaxIterationsProvider PcgMaxIterationsProvider { get; set; } = new PercentageMaxIterationsProvider(1.0);

			public AmgAiSolver2 BuildSolver()
			{
				var pcgFactory = new PcgAlgorithm.Factory();
				pcgFactory.ResidualTolerance = PcgConvergenceTolerance;
				pcgFactory.MaxIterationsProvider = PcgMaxIterationsProvider;
				var pcgAlgorithm = pcgFactory.Build();

				var initialPreconditioner = new JacobiPreconditioner();

				var smoothing = new MultigridLevelSmoothing()
					.AddPreSmoother(new GaussSeidelIterationCsr(forwardDirection: true), 1)
					.AddPreSmoother(new GaussSeidelIterationCsr(forwardDirection: false), 1)
					.SetPostSmoothersSameAsPreSmoothers();
				var amgPreconditioner = new PodAmgPreconditioner(
					keepOnlyNonZeroPrincipalComponents: true, smoothing, numIterations: 1);

				return new AmgAiSolver2(DofOrderer, pcgAlgorithm, MatrixPatternWillNotBeModified, initialPreconditioner,
					amgPreconditioner, numParameterSetsForPod, numPrincipalComponentsInPod/*, surrogateBuilder.BuildSurrogate()*/);
			}
		}
	}
}
