namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Algorithms;
	using MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy;

	public class SaAmgSolverBuilder
	{
		private readonly IDofOrderer dofOrderer;
		private readonly SaAmgAlgorithm.Builder multigridBuilder;

		private IProblemDependentNullspaceCandidateSelectionStrategy nullspaceCandidateSelection;

		public SaAmgSolverBuilder(SaAmgAlgorithm.Builder multigridBuilder)
		{
			this.multigridBuilder = multigridBuilder;
		}

		public void ConfigureForPoisson()
		{
			multigridBuilder.NullspaceCandidatesSelection = new ConstantCandidateSelection();
		}

		public void ConfigureForElasticity2D(IDofType[] translationDofs, double[]? referencePoint)
		{
			nullspaceCandidateSelection = new Elasticity2DCartesianCandidateSelection(translationDofs, referencePoint);
		}

		public void ConfigureForElasticity3D(IDofType[] translationDofs, double[]? referencePoint)
		{
			nullspaceCandidateSelection = new Elasticity3DCartesianCandidateSelection(translationDofs, referencePoint);
		}

		public IDofOrderer DofOrderer { get; set; } = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());

		public double DropToleranceForSmallEntriesOfSystemMatrix { get; set; } = -1;

		public StandaloneMultigridSolver BuildStandaloneSolver(Model model, IDofType[] dofsPerNode)
		{
			GlobalAlgebraicModel<CsrMatrix > algebraicModel = BuildAlgebraicModel(model);
			
			if (nullspaceCandidateSelection != null)
			{
				nullspaceCandidateSelection.SetModel(model, algebraicModel);
				multigridBuilder.NullspaceCandidatesSelection = nullspaceCandidateSelection;
			}

			SaAmgAlgorithm mg = multigridBuilder.Build();
			return new StandaloneMultigridSolver(algebraicModel, mg);
		}

		public PcgSolverWithMultigridPreconditioner BuildPcgMultigridSolver(Model model, IDofType[] dofsPerNode, PcgAlgorithm pcg)
		{
			GlobalAlgebraicModel<CsrMatrix> algebraicModel = BuildAlgebraicModel(model);

			if (nullspaceCandidateSelection != null)
			{
				nullspaceCandidateSelection.SetModel(model, algebraicModel);
				multigridBuilder.NullspaceCandidatesSelection = nullspaceCandidateSelection;
			}

			SaAmgAlgorithm mg = multigridBuilder.Build();

			return new PcgSolverWithMultigridPreconditioner(algebraicModel, mg, pcg);
		}

		private GlobalAlgebraicModel<CsrMatrix> BuildAlgebraicModel(Model model)
		{
			var assembler = new CsrMatrixAssembler(isMatrixSymmetric: true);
			assembler.SortColsOfEachRow = true;
			assembler.DropEntryTolerance = DropToleranceForSmallEntriesOfSystemMatrix;

			return new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, assembler);
		}
	}
}
