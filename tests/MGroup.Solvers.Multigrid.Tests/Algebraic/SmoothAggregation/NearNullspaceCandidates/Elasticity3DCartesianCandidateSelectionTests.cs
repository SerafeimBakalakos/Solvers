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
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Tests.Mocks;

	using Xunit;

	public static class Elasticity3DCartesianCandidateSelectionTests
	{
		[Fact]
		public static void TestChooseCandidateVectors_Cantilever3D()
		{
			int numElementsY = 2;
			(DokRowMajor dokA, _) = Cantilever3DElasticityProblem.CreateLinearSystem(numElementsY);
			CsrMatrix systemMatrix = dokA.BuildCsrMatrix(sortColsOfEachRow: true);

			double Lx = Cantilever3DElasticityProblem.LengthX;
			double Ly = Cantilever3DElasticityProblem.LengthY;
			double Lz = Cantilever3DElasticityProblem.LengthZ;

			double[] centroid = [Lx / 2.0, Ly / 2.0, Lz / 2.0];
			IDofType[] translationDofs = [StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ];
			var candidateSelectionStrategy = new Elasticity3DCartesianCandidateSelection(translationDofs, centroid);

			IModel model = Cantilever3DElasticityProblem.CreateModel(numElementsY);
			IGlobalAlgebraicModel algebraicModel = CreateAlgebraicModelAndOrderDofs(model);

			candidateSelectionStrategy.SetModel(model, algebraicModel);
			Matrix computedB = candidateSelectionStrategy.ChooseCandidateVectors(systemMatrix);

			var expectedB = Matrix.CreateFromArray(GetExpectedNullspaceCandidates());
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

		private static double[,] GetExpectedNullspaceCandidates()
		{
			return new double[,]
			{
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, -1 },
				{ 0, 0, 1, -0.5, 1, 0 },
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, -0.5 },
				{ 0, 0, 1, -0.5, 0.5, 0 },
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, 0 },
				{ 0, 0, 1, -0.5, -0, 0 },
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, 0.5 },
				{ 0, 0, 1, -0.5, -0.5, 0 },
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, 1 },
				{ 0, 0, 1, -0.5, -1, 0 },
				{ 1, 0, 0, 0, -1, 0.5 },
				{ 0, 1, 0, 1, 0, 1.5 },
				{ 0, 0, 1, -0.5, -1.5, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, -1 },
				{ 0, 0, 1, 0, 1, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, -0.5 },
				{ 0, 0, 1, 0, 0.5, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, 0 },
				{ 0, 0, 1, 0, -0, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, 0.5 },
				{ 0, 0, 1, 0, -0.5, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, 1 },
				{ 0, 0, 1, 0, -1, 0 },
				{ 1, 0, 0, 0, -1, -0 },
				{ 0, 1, 0, 1, 0, 1.5 },
				{ 0, 0, 1, 0, -1.5, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, -1 },
				{ 0, 0, 1, 0.5, 1, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, -0.5 },
				{ 0, 0, 1, 0.5, 0.5, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, 0 },
				{ 0, 0, 1, 0.5, -0, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, 0.5 },
				{ 0, 0, 1, 0.5, -0.5, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, 1 },
				{ 0, 0, 1, 0.5, -1, 0 },
				{ 1, 0, 0, 0, -1, -0.5 },
				{ 0, 1, 0, 1, 0, 1.5 },
				{ 0, 0, 1, 0.5, -1.5, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, -1 },
				{ 0, 0, 1, -0.5, 1, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, -0.5 },
				{ 0, 0, 1, -0.5, 0.5, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, 0 },
				{ 0, 0, 1, -0.5, -0, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, 0.5 },
				{ 0, 0, 1, -0.5, -0.5, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, 1 },
				{ 0, 0, 1, -0.5, -1, 0 },
				{ 1, 0, 0, 0, -0.5, 0.5 },
				{ 0, 1, 0, 0.5, 0, 1.5 },
				{ 0, 0, 1, -0.5, -1.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, -1 },
				{ 0, 0, 1, 0, 1, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, -0.5 },
				{ 0, 0, 1, 0, 0.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, 0 },
				{ 0, 0, 1, 0, -0, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, 0.5 },
				{ 0, 0, 1, 0, -0.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, 1 },
				{ 0, 0, 1, 0, -1, 0 },
				{ 1, 0, 0, 0, -0.5, -0 },
				{ 0, 1, 0, 0.5, 0, 1.5 },
				{ 0, 0, 1, 0, -1.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, -1 },
				{ 0, 0, 1, 0.5, 1, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, -0.5 },
				{ 0, 0, 1, 0.5, 0.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, 0 },
				{ 0, 0, 1, 0.5, -0, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, 0.5 },
				{ 0, 0, 1, 0.5, -0.5, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, 1 },
				{ 0, 0, 1, 0.5, -1, 0 },
				{ 1, 0, 0, 0, -0.5, -0.5 },
				{ 0, 1, 0, 0.5, 0, 1.5 },
				{ 0, 0, 1, 0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, -1 },
				{ 0, 0, 1, -0.5, 1, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, -0.5 },
				{ 0, 0, 1, -0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, 0 },
				{ 0, 0, 1, -0.5, -0, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, 0.5 },
				{ 0, 0, 1, -0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, 1 },
				{ 0, 0, 1, -0.5, -1, 0 },
				{ 1, 0, 0, 0, 0, 0.5 },
				{ 0, 1, 0, -0, 0, 1.5 },
				{ 0, 0, 1, -0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, -1 },
				{ 0, 0, 1, 0, 1, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, -0.5 },
				{ 0, 0, 1, 0, 0.5, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, 0 },
				{ 0, 0, 1, 0, -0, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, 0.5 },
				{ 0, 0, 1, 0, -0.5, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, 1 },
				{ 0, 0, 1, 0, -1, 0 },
				{ 1, 0, 0, 0, 0, -0 },
				{ 0, 1, 0, -0, 0, 1.5 },
				{ 0, 0, 1, 0, -1.5, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, -1 },
				{ 0, 0, 1, 0.5, 1, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, -0.5 },
				{ 0, 0, 1, 0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, 0 },
				{ 0, 0, 1, 0.5, -0, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, 0.5 },
				{ 0, 0, 1, 0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, 1 },
				{ 0, 0, 1, 0.5, -1, 0 },
				{ 1, 0, 0, 0, 0, -0.5 },
				{ 0, 1, 0, -0, 0, 1.5 },
				{ 0, 0, 1, 0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, -1 },
				{ 0, 0, 1, -0.5, 1, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, -0.5 },
				{ 0, 0, 1, -0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, 0 },
				{ 0, 0, 1, -0.5, -0, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, 0.5 },
				{ 0, 0, 1, -0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, 1 },
				{ 0, 0, 1, -0.5, -1, 0 },
				{ 1, 0, 0, 0, 0.5, 0.5 },
				{ 0, 1, 0, -0.5, 0, 1.5 },
				{ 0, 0, 1, -0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, -1 },
				{ 0, 0, 1, 0, 1, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, -0.5 },
				{ 0, 0, 1, 0, 0.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, 0 },
				{ 0, 0, 1, 0, -0, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, 0.5 },
				{ 0, 0, 1, 0, -0.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, 1 },
				{ 0, 0, 1, 0, -1, 0 },
				{ 1, 0, 0, 0, 0.5, -0 },
				{ 0, 1, 0, -0.5, 0, 1.5 },
				{ 0, 0, 1, 0, -1.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, -1 },
				{ 0, 0, 1, 0.5, 1, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, -0.5 },
				{ 0, 0, 1, 0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, 0 },
				{ 0, 0, 1, 0.5, -0, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, 0.5 },
				{ 0, 0, 1, 0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, 1 },
				{ 0, 0, 1, 0.5, -1, 0 },
				{ 1, 0, 0, 0, 0.5, -0.5 },
				{ 0, 1, 0, -0.5, 0, 1.5 },
				{ 0, 0, 1, 0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, -1 },
				{ 0, 0, 1, -0.5, 1, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, -0.5 },
				{ 0, 0, 1, -0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, 0 },
				{ 0, 0, 1, -0.5, -0, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, 0.5 },
				{ 0, 0, 1, -0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, 1 },
				{ 0, 0, 1, -0.5, -1, 0 },
				{ 1, 0, 0, 0, 1, 0.5 },
				{ 0, 1, 0, -1, 0, 1.5 },
				{ 0, 0, 1, -0.5, -1.5, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, -1 },
				{ 0, 0, 1, 0, 1, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, -0.5 },
				{ 0, 0, 1, 0, 0.5, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, 0 },
				{ 0, 0, 1, 0, -0, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, 0.5 },
				{ 0, 0, 1, 0, -0.5, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, 1 },
				{ 0, 0, 1, 0, -1, 0 },
				{ 1, 0, 0, 0, 1, -0 },
				{ 0, 1, 0, -1, 0, 1.5 },
				{ 0, 0, 1, 0, -1.5, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, -1 },
				{ 0, 0, 1, 0.5, 1, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, -0.5 },
				{ 0, 0, 1, 0.5, 0.5, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, 0 },
				{ 0, 0, 1, 0.5, -0, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, 0.5 },
				{ 0, 0, 1, 0.5, -0.5, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, 1 },
				{ 0, 0, 1, 0.5, -1, 0 },
				{ 1, 0, 0, 0, 1, -0.5 },
				{ 0, 1, 0, -1, 0, 1.5 },
				{ 0, 0, 1, 0.5, -1.5, 0 }
			};
		}
	}
}
