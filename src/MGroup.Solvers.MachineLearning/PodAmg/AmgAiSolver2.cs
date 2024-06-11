namespace MGroup.Solvers.MachineLearning.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;
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
	using MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg;
	using MGroup.LinearAlgebra.AlgebraicMultiGrid;

	public class AmgAiSolver2 : ISolver
	{
		public enum Subtask 
		{ 
			CreatePreconditioner, SolveWithPcg
		}

		private enum Stage { Start, CreateInitPrecond, UseInitPrecond, CreateMLPrecond, UseMLPrecond }

		private const string name = "POD-AMG solver"; // for error messages

		private readonly IDofOrderer dofOrderer;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly IPreconditioner initialPreconditioner;
		private readonly PodAmgPreconditioner amgPreconditioner;
		private readonly bool matrixPatternWillNotBeModified;
		private readonly int numParameterSetsBeforeTraining;
		private readonly int numPrincipalComponentsInPod;

		private Stage currentStage;
		private double[] modelParametersCurrent;
		//private CaeFffnSurrogate surrogate;

		private int currentParameterSetIdx;
		private int currentParameterSetId;
		private int currentTimeStep;

		private AmgAiSolver2(IDofOrderer dofOrderer, PcgAlgorithm pcgAlgorithm, bool matrixPatternWillNotBeModified,
			IPreconditioner initialPreconditioner, PodAmgPreconditioner amgPreconditioner,
			int numParameterSetsBeforeTraining, int numPrincipalComponentsInPod/*, CaeFffnSurrogate surrogate*/)
		{
			this.dofOrderer = dofOrderer;
			this.pcgAlgorithm = pcgAlgorithm;
			this.matrixPatternWillNotBeModified = matrixPatternWillNotBeModified;
			this.initialPreconditioner = initialPreconditioner;
			this.amgPreconditioner = amgPreconditioner;
			this.numParameterSetsBeforeTraining = numParameterSetsBeforeTraining;
			this.numPrincipalComponentsInPod = numPrincipalComponentsInPod;
			//this.surrogate = surrogate;

			currentStage = Stage.Start;
			currentParameterSetIdx = -1;
			Logger = new SolverLogger(name);
		}

		public GlobalAlgebraicModel<CsrMatrix> AlgebraicModel { get; private set; }

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; private set; }

		ISolverLogger ISolver.Logger => Logger;

		public SolverLogger Logger { get; }

		public string Name => name;

		public string CurrentPreconditionerName 
			=> currentStage == Stage.UseMLPrecond ? "POD-2D preconditioner" : initialPreconditioner.ToString();

		public SolutionDatabase2 SavedSolutions { get; } = new SolutionDatabase2(ensureSameLengthVectors: true);


		public void HandleMatrixWillBeSet() { }

		public void Initialize() { }

		public Matrix InverseSystemMatrixTimesOtherMatrix(IMatrixView otherMatrix) => throw new NotImplementedException();

		public void PreventFromOverwrittingSystemMatrices()
		{
			// No factorization is done.
		}

		public void SetModel(int parameterSetId, double[] modelParameters, IModel model)
		{
			// New analysis. Restart the algebraic objects used
			AlgebraicModel = new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, new CsrMatrixAssembler(true));
			this.LinearSystem = AlgebraicModel.LinearSystem;
			if (this.LinearSystem.Solution != null)
			{
				this.LinearSystem.Solution.Clear();
			}
			this.LinearSystem.Observers.Add(this);
			((SolverLogger)Logger).Clear();

			// Restart tracking
			currentTimeStep = 0;
			++currentParameterSetIdx;
			currentParameterSetId = parameterSetId;
			modelParametersCurrent = modelParameters.Copy();

			// Manage solver stage
			if (currentParameterSetIdx < numParameterSetsBeforeTraining)
			{ 
				SavedSolutions.SaveModelParameters(parameterSetId, modelParameters);
			}
			else if (currentParameterSetIdx == numParameterSetsBeforeTraining)
			{
				currentStage = Stage.CreateMLPrecond;
			}
		}

		public void Solve()
		{
			if (currentStage == Stage.Start)
			{
				currentStage = Stage.CreateInitPrecond;
				CreateInitialPreconditioner();
				currentStage = Stage.UseInitPrecond;
			}

			if (currentStage == Stage.UseInitPrecond)
			{
				Vector solution = SolveUsingInitialPreconditioner();
				SavedSolutions.SaveSolution(currentParameterSetId, currentTimeStep, LinearSystem.Solution.SingleVector);
			}

			if (currentStage == Stage.CreateMLPrecond)
			{
				CreateMLPreconditioner();
				currentStage = Stage.UseMLPrecond;
			}

			if (currentStage == Stage.UseMLPrecond)
			{
				SolveUsingPodAmgPreconditioner();
			}

			++currentTimeStep;
		}

		private void CreateInitialPreconditioner()
		{
			Console.WriteLine("******************* Calculate initial preconditioner ********************************");
			var watch = new Stopwatch();
			watch.Start();

			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			initialPreconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);
			
			watch.Stop();
			Logger.LogTaskDuration(Subtask.CreatePreconditioner.ToString(), watch.ElapsedMilliseconds);
		}

		private void CreateMLPreconditioner()
		{
			Console.WriteLine("******************* Create ML preconditioner ********************************");
			var watch = new Stopwatch();
			watch.Start();

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
			amgPreconditioner.UpdateMatrix(LinearSystem.Matrix.SingleMatrix, !matrixPatternWillNotBeModified);

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

			watch.Stop();
			Logger.LogTaskDuration(Subtask.CreatePreconditioner.ToString(), watch.ElapsedMilliseconds);
		}

		private Vector SolveUsingInitialPreconditioner()
		{
			var watch = new Stopwatch();
			watch.Start();

			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;

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

			watch.Stop();
			Logger.LogTaskDuration(Subtask.SolveWithPcg.ToString(), watch.ElapsedMilliseconds);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);

			return LinearSystem.Solution.SingleVector.Copy();
		}

		private void SolveUsingPodAmgPreconditioner()
		{
			var watch = new Stopwatch();
			watch.Start();

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
			//also set true to false in 290


			IterativeStatistics stats = pcgAlgorithm.Solve(matrix, amgPreconditioner, rhs, LinearSystem.Solution.SingleVector,
				true, () => Vector.CreateZero(systemSize));
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm with "
					+ $"AMG-POD preconditioner run for {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}
			
			watch.Stop();
			Logger.LogTaskDuration(Subtask.SolveWithPcg.ToString(), watch.ElapsedMilliseconds);
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
