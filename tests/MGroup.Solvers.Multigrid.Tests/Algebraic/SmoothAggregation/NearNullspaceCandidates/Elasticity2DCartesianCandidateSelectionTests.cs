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
		public static void TestForCantilever2D()
		{
			int numElementsY = 2;
			IModel model = ElasticityCantilever2DProblem.CreateModel(numElementsY);
			CsrMatrix systemMatrix = Cantilever2D_4x2Elements_SaAmgResults.GetSystemMatrix(0);
			double[] domainCentroid = [ElasticityCantilever2DProblem.LengthX / 2.0, ElasticityCantilever2DProblem.LengthY / 2.0];
			ISubdomainFreeDofOrdering dofOrdering = ElasticityCantilever2DProblem.OrderDofs(numElementsY);

			var candidatesSelection = new Elasticity2DCartesianCandidateSelection(model, dofOrdering, StructuralDof.TranslationX, StructuralDof.TranslationY, domainCentroid);
			Matrix computedB = candidatesSelection.ChooseCandidateVectors(systemMatrix);

			var expectedB = Matrix.CreateFromArray(Cantilever2D_4x2Elements_SaAmgResults.GetNullspaceCandidates(0, "None"));

			var comparer = new MatrixComparer(1E-30);
			comparer.AssertEqual(expectedB, computedB);
		}
	}
}
