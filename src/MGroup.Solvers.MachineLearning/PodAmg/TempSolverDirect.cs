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
	using MGroup.MSolve.Solution.AlgebraicModel;

	public class TempSolverDirect: SingleSubdomainSolverBase<SkylineMatrix>, ITempSolver
	{
		private readonly double factorizationPivotTolerance;

		private bool factorizeInPlace = true;
		private bool mustFactorize = true;
		private LdlSkyline factorizedMatrix;

		private int currentParameterSet;
		private int currentTimeStep;

		private TempSolverDirect(GlobalAlgebraicModel<SkylineMatrix> model, double factorizationPivotTolerance)
			: base(model, "TempAiSolver")
		{
			this.factorizationPivotTolerance = factorizationPivotTolerance;
		}

		public IAlgebraicModel Model => model;

		public SolutionDatabase SavedSolutions { get; } = new SolutionDatabase();

		public override void HandleMatrixWillBeSet()
		{
			mustFactorize = true;
			factorizedMatrix = null;
		}

		public override void Initialize() { }

		public void OnModelParameterUpdate(int parameterSet)
		{
			currentParameterSet = parameterSet;
			currentTimeStep = 0;
		}

		public override void PreventFromOverwrittingSystemMatrices() => factorizeInPlace = false;

		/// <summary>
		/// Solves the linear system with back-forward substitution. If the matrix has been modified, it will be refactorized.
		/// </summary>
		public override void Solve()
		{
			Console.WriteLine("new timestep");
			var watch = new Stopwatch();
			SkylineMatrix matrix = LinearSystem.Matrix.SingleMatrix;
			int systemSize = matrix.NumRows;
			if (LinearSystem.Solution.SingleVector == null)
			{
				LinearSystem.Solution.SingleVector = Vector.CreateZero(systemSize);
			}
			else LinearSystem.Solution.Clear();// no need to waste computational time on this in a direct solver

			// Factorization
			if (mustFactorize)
			{
				watch.Start();
				factorizedMatrix = matrix.FactorLdl(factorizeInPlace, factorizationPivotTolerance);
				watch.Stop();
				Logger.LogTaskDuration("Matrix factorization", watch.ElapsedMilliseconds);
				watch.Reset();
				mustFactorize = false;
			}

			// Substitutions
			watch.Start();
			factorizedMatrix.SolveLinearSystem(LinearSystem.RhsVector.SingleVector, LinearSystem.Solution.SingleVector);
			watch.Stop();
			Logger.LogTaskDuration("Back/forward substitutions", watch.ElapsedMilliseconds);
			Logger.IncrementAnalysisStep();

			SavedSolutions.SaveSolution(currentParameterSet, currentTimeStep, LinearSystem.Solution);
			++currentTimeStep;
		}


		protected override Matrix InverseSystemMatrixTimesOtherMatrix(IMatrixView otherMatrix)
		{
			throw new NotImplementedException();
		}

		public class Factory
		{
			public Factory() { }

			public IDofOrderer DofOrderer { get; set; }
				= new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

			public double FactorizationPivotTolerance { get; set; } = 1E-15;

			public TempSolverDirect BuildSolver(GlobalAlgebraicModel<SkylineMatrix> model)
			{
				return new TempSolverDirect(model, FactorizationPivotTolerance);
			}

			public GlobalAlgebraicModel<SkylineMatrix> BuildAlgebraicModel(IModel model)
				=> new GlobalAlgebraicModel<SkylineMatrix>(model, DofOrderer, new SkylineMatrixAssembler());
		}
	}
}
