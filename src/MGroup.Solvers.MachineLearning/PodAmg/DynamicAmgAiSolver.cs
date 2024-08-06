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
	using MGroup.Solvers.MachineLearning.PodAmg.Surrogates;
	using System.IO;

	public class DynamicAmgAiSolver : ISolver
	{
		public enum Subtask 
		{ 
			UpdatePreconditioner, SolveWithPcg, TrainML
		}

		private enum Stage 
		{ 
			Start, UpdateInitPrecond, SolveWithInitPrecond, TrainMLModels, UpdateMLPrecondForNewModel, SolveWithMLPrecond
		}

		private const string name = "POD-AMG solver"; // for error messages

		private readonly IDofOrderer dofOrderer;
		private readonly PcgAlgorithm pcgAlgorithm;
		private readonly bool matrixPatternWillNotBeModified;
		private readonly IPreconditioner initialPreconditioner;
		private readonly bool alwaysUseInitialPreconditioner;
		private readonly IDynamicMLPreconditioner mlPreconditioner;
		private readonly ISolutionTrainingStrategy trainingStrategy;
		private readonly ISolutionPredictionStrategy solutionPrediction;
		private readonly int numParameterSetsBeforeTraining;
		private readonly int numPrincipalComponentsInPod;

		private Stage currentStage;
		private double[] modelParametersCurrent;
		private int currentParameterSetIdx;
		private int currentParameterSetId;
		private int currentTimeStep;

		private DynamicAmgAiSolver(IDofOrderer dofOrderer, PcgAlgorithm pcgAlgorithm, bool matrixPatternWillNotBeModified,
			IPreconditioner initialPreconditioner, IDynamicMLPreconditioner mlPreconditioner, 
			ISolutionTrainingStrategy trainingStrategy, int numParameterSetsBeforeTraining, int numPrincipalComponentsInPod,
			ISolutionPredictionStrategy solutionPrediction, bool alwaysUseInitialPreconditioner)
		{
			this.dofOrderer = dofOrderer;
			this.pcgAlgorithm = pcgAlgorithm;
			this.matrixPatternWillNotBeModified = matrixPatternWillNotBeModified;
			this.trainingStrategy = trainingStrategy;
			this.initialPreconditioner = initialPreconditioner;
			this.mlPreconditioner = mlPreconditioner;
			this.numParameterSetsBeforeTraining = numParameterSetsBeforeTraining;
			this.numPrincipalComponentsInPod = numPrincipalComponentsInPod;
			this.solutionPrediction = solutionPrediction;
			this.alwaysUseInitialPreconditioner = alwaysUseInitialPreconditioner;
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
			=> currentStage == Stage.SolveWithMLPrecond ? "POD-2G preconditioner" : initialPreconditioner.GetType().Name;

		public SolutionDatabaseDynamic SavedSolutions { get; private set; } = new SolutionDatabaseDynamic();

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
				currentStage = Stage.UpdateInitPrecond;
			}
			else if (currentParameterSetIdx > numParameterSetsBeforeTraining)
			{
				currentStage = Stage.UpdateMLPrecondForNewModel;
			}
			else
			{
				currentStage = Stage.TrainMLModels;
			}
		}

		public void Solve()
		{
			//PrintProgress();
			if (currentStage == Stage.Start)
			{
				throw new InvalidOperationException("The model parameters must be set before calling this.");
			}

			if (currentStage == Stage.UpdateInitPrecond)
			{
				UpdateInitialPreconditioner();
				currentStage = Stage.SolveWithInitPrecond;
			}

			if (currentStage == Stage.SolveWithInitPrecond)
			{
				Vector solution = SolveUsingInitialPreconditioner();
				if (trainingStrategy.MustSaveSolution(currentTimeStep) || solutionPrediction.MustSaveSolution(currentTimeStep))
				{
					SavedSolutions.SaveSolution(currentParameterSetId, currentTimeStep, LinearSystem.Solution.SingleVector);
				}
			}

			if (currentStage == Stage.TrainMLModels)
			{
				if (!alwaysUseInitialPreconditioner)
				{
					TrainMLModels();
				}
				currentStage = Stage.UpdateMLPrecondForNewModel;
			}

			if (currentStage == Stage.UpdateMLPrecondForNewModel)
			{
				if (alwaysUseInitialPreconditioner)
				{
					UpdateInitialPreconditioner();
				}
				else
				{
					UpdateMLPreconditionerForNewModel();
				}
				currentStage = Stage.SolveWithMLPrecond;
			}

			if (currentStage == Stage.SolveWithMLPrecond)
			{
				UpdateMLPreconditionerForNewTimeStep();
				Vector solution = alwaysUseInitialPreconditioner ? 
					SolveUsingInitialPreconditioner() : SolveUsingPodAmgPreconditioner();
				SavedSolutions.SaveCurrentSolutionOnly(solution);
			}

			++currentTimeStep;
		}

		public void LoadState(string directory)
		{
			string pathSerializedSolver = Path.Combine(directory, "serialized_solver");
			string pathSerializedDB = Path.Combine(directory, "serialized_solutionsDB");
			if (!(File.Exists(pathSerializedSolver) && File.Exists(pathSerializedDB)))
			{
				throw new IOException($"Invalid files: {pathSerializedSolver}, {pathSerializedDB}");
			}

			// State of this object
			State state = null;
			using (Stream stream = File.Open(pathSerializedSolver, FileMode.Open))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				state = (State)(binaryFormatter.Deserialize(stream));
			}
			currentTimeStep = state.CurrentTimeStep;
			currentStage = state.CurrentStage;
			currentParameterSetId = state.CurrentParameterSetId;
			currentParameterSetIdx = state.CurrentParameterSetIdx;
			modelParametersCurrent = state.ModelParametersCurrent;

			// State of solution DB
			SolutionDatabaseDynamic solutionDB = null;
			using (Stream stream = File.Open(pathSerializedDB, FileMode.Open))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				solutionDB = (SolutionDatabaseDynamic)(binaryFormatter.Deserialize(stream));
			}
			this.SavedSolutions = solutionDB;
		}

		public void SaveState(string directory)
		{
			if (!Directory.Exists(directory))
			{
				throw new IOException($"Invalid directory: {directory}");
			}

			string pathSerializedSolver = Path.Combine(directory, "serialized_solver");
			string pathSerializedDB = Path.Combine(directory, "serialized_solutionsDB");

			// State of this object
			var state = new State();
			state.CurrentTimeStep = currentTimeStep;
			state.CurrentStage = currentStage;
			state.CurrentParameterSetId = currentParameterSetId;
			state.CurrentParameterSetIdx = currentParameterSetIdx;
			state.ModelParametersCurrent = modelParametersCurrent;
			using (Stream stream = File.Open(pathSerializedSolver, FileMode.Create))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(stream, state);
			}

			// State of solution DB
			using (Stream stream = File.Open(pathSerializedDB, FileMode.Create))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(stream, SavedSolutions);
			}
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

		private Vector SolveUsingPodAmgPreconditioner()
		{
			var watch = new Stopwatch();
			watch.Start();

			CsrMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;
			Vector rhs = LinearSystem.RhsVector.SingleVector;

			// Use ML prediction as initial guess.
			double[] prediction = solutionPrediction.Predict(currentTimeStep, modelParametersCurrent);
			LinearSystem.Solution.SingleVector.CopyFrom(Vector.CreateFromArray(prediction));

			IterativeStatistics stats = pcgAlgorithm.Solve(matrix, mlPreconditioner, rhs, LinearSystem.Solution.SingleVector,
				false, () => Vector.CreateZero(systemSize));
			if (!stats.HasConverged)
			{
				throw new IterativeSolverNotConvergedException(Name + " did not converge to a solution. PCG algorithm with "
					+ $"AMG-POD preconditioner run for {stats.NumIterationsRequired} iterations and the residual norm ratio was"
					+ $" {stats.ResidualNormRatioEstimation}");
			}
			
			watch.Stop();
			Logger.LogTaskDuration(Subtask.SolveWithPcg.ToString(), watch.ElapsedMilliseconds);
			Logger.LogIterativeAlgorithm(stats.NumIterationsRequired, stats.ResidualNormRatioEstimation);

			return LinearSystem.Solution.SingleVector;
		}

		private void UpdateInitialPreconditioner()
		{
			var watch = new Stopwatch();
			watch.Start();

			IMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			initialPreconditioner.UpdateMatrix(matrix, !matrixPatternWillNotBeModified);

			watch.Stop();
			Logger.LogTaskDuration(Subtask.UpdatePreconditioner.ToString(), watch.ElapsedMilliseconds);
		}

		private void UpdateMLPreconditionerForNewModel()
		{
			var watch = new Stopwatch();
			watch.Start();

			mlPreconditioner.UpdateMatrix(LinearSystem.Matrix.SingleMatrix, !matrixPatternWillNotBeModified);

			watch.Stop();
			Logger.LogTaskDuration(Subtask.UpdatePreconditioner.ToString(), watch.ElapsedMilliseconds);
		}

		private void UpdateMLPreconditionerForNewTimeStep()
		{
			var watch = new Stopwatch();
			watch.Start();

			mlPreconditioner.UpdateForTimeStep(currentTimeStep);

			watch.Stop();
			Logger.LogTaskDuration(Subtask.UpdatePreconditioner.ToString(), watch.ElapsedMilliseconds);
		}

		private void TrainMLModels()
		{
			Console.WriteLine();
			Console.Write("ML training: start ...");
			var watch = new Stopwatch();
			watch.Start();

			// Train the ML preconditioner
			int numDofs = AlgebraicModel.LinearSystem.Solution.Length;
			mlPreconditioner.Initialize(numDofs, numPrincipalComponentsInPod, SavedSolutions);

			// Train the CAE-FFNN surrogate for generating initial solution guesses
			solutionPrediction.Train(SavedSolutions);

			// Free up some memory by deleting the stored solution vectors
			SavedSolutions.Clear();

			watch.Stop();
			Logger.LogTaskDuration(Subtask.TrainML.ToString(), watch.ElapsedMilliseconds);
			Console.WriteLine(" end");
		}

		private void PrintProgress()
		{
			Console.WriteLine($"Parameter set = {currentParameterSetId}, time step = {currentTimeStep}");
		}

		public class Factory
		{
			private readonly int numParameterSetsForPod;
			private readonly int numPrincipalComponentsInPod;
			private readonly ISolutionPredictionStrategy solutionPrediction;

			public Factory(int numParameterSetsForPod, int numPrincipalComponentsInPod,
				ISolutionPredictionStrategy solutionPrediction)
			{
				this.numParameterSetsForPod = numParameterSetsForPod;
				this.numPrincipalComponentsInPod = numPrincipalComponentsInPod;
				this.solutionPrediction = solutionPrediction;
			}

			public bool AlwaysUseInitialPreconditioner { get; set; } = false;

			public IDofOrderer DofOrderer { get; set; }
				= new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

			public bool KeepOnlyNonZeroPrincipalComponents { get; set; } = true;

			public bool MatrixPatternWillNotBeModified { get; set; } = false;

			public IPcgResidualConvergence PcgConvergenceStrategy { get; set; } = new RegularPcgConvergence();

			public double PcgConvergenceTolerance { get; set; } = 1E-5;

			public IMaxIterationsProvider PcgMaxIterationsProvider { get; set; } = new PercentageMaxIterationsProvider(1.0);

			public ISolutionTrainingStrategy TrainingStrategy { get; set; } 
				= new BulkSolutionsTrainingStrategy(timeStepSavePeriod: 1);


			public DynamicAmgAiSolver BuildSolver()
			{
				var pcgFactory = new PcgAlgorithm.Factory();
				pcgFactory.ResidualTolerance = PcgConvergenceTolerance;
				pcgFactory.MaxIterationsProvider = PcgMaxIterationsProvider;
				pcgFactory.Convergence = PcgConvergenceStrategy;
				var pcgAlgorithm = pcgFactory.Build();

				var initialPreconditioner = new JacobiPreconditioner();

				var smoothing = new MultigridLevelSmoothing()
					.AddPreSmoother(new GaussSeidelIterationCsr(forwardDirection: true), 1)
					.AddPreSmoother(new GaussSeidelIterationCsr(forwardDirection: false), 1)
					.SetPostSmoothersSameAsPreSmoothers();
				var podAmgPreconditioner = new PodAmgPreconditioner(
					KeepOnlyNonZeroPrincipalComponents, smoothing, numIterations: 1);

				IDynamicMLPreconditioner mlPreconditioner = TrainingStrategy.CreatePreconditioner(podAmgPreconditioner);

				return new DynamicAmgAiSolver(DofOrderer, pcgAlgorithm, MatrixPatternWillNotBeModified, initialPreconditioner,
					mlPreconditioner, TrainingStrategy, numParameterSetsForPod, numPrincipalComponentsInPod, solutionPrediction,
					AlwaysUseInitialPreconditioner);
			}
		}

		[Serializable]
		private class State
		{
			public Stage CurrentStage { get; set; }
			public double[] ModelParametersCurrent { get; set; }
			public int CurrentParameterSetIdx { get; set; }
			public int CurrentParameterSetId { get; set; }
			public int CurrentTimeStep { get; set; }
		}
	}
}
