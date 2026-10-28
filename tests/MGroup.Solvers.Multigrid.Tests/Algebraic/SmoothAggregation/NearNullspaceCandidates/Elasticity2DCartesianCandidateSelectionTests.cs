namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.Assemblers;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.DofOrdering.Reordering;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Tests.Mocks;

	using Xunit;

	public static class Elasticity2DCartesianCandidateSelectionTests
	{
		[Fact]
		public static void TestChooseCandidateVectors_Cantilever8x4Elements()
		{
			CsrMatrix systemMatrix = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(0);
			double Lx = Cantilever2DElasticityProblem.LengthX;
			double Ly = Cantilever2DElasticityProblem.LengthY;

			double[] centroid = [Lx / 2.0, Ly / 2.0];
			IDofType[] translationDofs = [StructuralDof.TranslationX, StructuralDof.TranslationY];
			var candidateSelectionStrategy = new Elasticity2DCartesianCandidateSelection(translationDofs, centroid);

			IModel model = Cantilever2DElasticityProblem.CreateModel(numElementsY: 4);
			IGlobalAlgebraicModel algebraicModel = CreateAlgebraicModelAndOrderDofs(model);
			
			candidateSelectionStrategy.SetModel(model, algebraicModel);
			Matrix computedB = candidateSelectionStrategy.ChooseCandidateVectors(systemMatrix);

			var expectedB = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(level: 0));
			var comparer = new MatrixComparer(1E-30);
			comparer.AssertEqual(expectedB, computedB);
		}

		private static IGlobalAlgebraicModel CreateAlgebraicModelAndOrderDofs(IModel model)
		{
			var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
			var assembler = new CsrMatrixAssembler(isMatrixSymmetric: true);
			assembler.SortColsOfEachRow = true;
			assembler.DropEntryTolerance = 1E-10;
			
			var algebraicModel = new GlobalAlgebraicModel<CsrMatrix>(model, dofOrderer, assembler);
			algebraicModel.BoundaryConditionsInterpreter = new MockBCInterpreter(model);
			algebraicModel.OrderDofs();
			
			return algebraicModel;
		}
	}
}
