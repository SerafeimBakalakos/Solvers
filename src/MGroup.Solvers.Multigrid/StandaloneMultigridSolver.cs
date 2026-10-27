namespace MGroup.Solvers.Multigrid
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Solution;
	using MGroup.MSolve.Solution.AlgebraicModel;
	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algorithms;

	public class StandaloneMultigridSolver : ISolver
	{
		private readonly GlobalAlgebraicModel<CsrMatrix> algebraicModel;
		private readonly IMultigridAlgorithm multigrid;

		private bool isInitialized = false;
		private bool systemMatrixHasChanged = true;

		public StandaloneMultigridSolver(GlobalAlgebraicModel<CsrMatrix> algebraicModel, IMultigridAlgorithm multigrid)
		{
			this.algebraicModel = algebraicModel;
			this.multigrid = multigrid;

			Name = "Solver based on " + multigrid.Name;
			Logger = multigrid.Logger;

			LinearSystem = algebraicModel.LinearSystem;
			LinearSystem.Observers.Add(this);
		}

		public IAlgebraicModel AlgebraicModel => algebraicModel;

		IGlobalLinearSystem ISolver.LinearSystem => LinearSystem;

		public ISolverLogger Logger { get; }

		public GlobalLinearSystem<CsrMatrix> LinearSystem { get; set; }

		public string Name { get; }

		public void HandleMatrixWillBeSet()
		{
			systemMatrixHasChanged = true;
		}

		public void Initialize()
		{
			throw new NotImplementedException("This method is not called by all analyzers. At least not by Linear or Static");
		}

		public void PreventFromOverwrittingSystemMatrices() { }

		public void Solve()
		{
			if (!isInitialized)
			{
				multigrid.Initialize();
				isInitialized = true;
			}

			if (systemMatrixHasChanged)
			{
				multigrid.PrepareForNewLinearSystemMatrix(LinearSystem.Matrix);
			}

			multigrid.SolveLinearSystem(LinearSystem.RhsVector, LinearSystem.Solution);
		}
	}
}
