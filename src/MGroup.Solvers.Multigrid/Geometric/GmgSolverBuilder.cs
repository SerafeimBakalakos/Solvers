namespace MGroup.Solvers.Multigrid.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algorithms;
	using MGroup.Solvers.Multigrid.Geometric.OperatorHierarchy;

	public class GmgSolverBuilder
	{
		private readonly IDofOrderer dofOrderer;
		private readonly GmgAlgorithm.Builder multigridBuilder;

		public GmgSolverBuilder(GmgAlgorithm.Builder multigridBuilder)
		{
			//TODO: Here is where I can enforce that Nodes are numbered in the same way prolongation matrices expect them to.
			//		Ideally I should just reorder them, and thus let the users number them however they want. 
			dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			this.multigridBuilder = multigridBuilder;
		}

		public double DropToleranceForSmallEntriesOfSystemMatrix { get; set; } = -1;

		public StandaloneMultigridSolver BuildStandaloneSolver(Model model, IDofType[] dofsPerNode)
		{
			GlobalAlgebraicModel<CsrMatrix > algebraicModel = BuildAlgebraicModel(model);
			var freeDofProvider = new StandardFreeDofProvider(model, dofsPerNode, () => algebraicModel.SubdomainFreeDofOrdering);
			GmgAlgorithm mg = multigridBuilder.Build(freeDofProvider);

			return new StandaloneMultigridSolver(algebraicModel, mg);
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
