namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class Elasticity2DCartesianCandidateSelectionTests
	{
		[Fact]
		public static void TestChooseCandidateVectors_Cantilever8x4Elements()
		{
			int numElementsY = 4;
			IModel model = Cantilever2DElasticityProblem.CreateModel(numElementsY);
			CsrMatrix systemMatrix = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(0);
			double[] domainCentroid = [Cantilever2DElasticityProblem.LengthX / 2.0, Cantilever2DElasticityProblem.LengthY / 2.0];
			ISubdomainFreeDofOrdering dofOrdering = Cantilever2DElasticityProblem.OrderDofs(numElementsY);

			var candidatesSelection = new Elasticity2DCartesianCandidateSelection(model, dofOrdering, StructuralDof.TranslationX, StructuralDof.TranslationY, domainCentroid);
			Matrix computedB = candidatesSelection.ChooseCandidateVectors(systemMatrix);

			var expectedB = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(level: 0));
			var comparer = new MatrixComparer(1E-30);
			comparer.AssertEqual(expectedB, computedB);
		}
	}
}
