namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class ConstantCandidateSelectionTests
	{
		[Fact]
		public static void TestForPoisson1D()
		{
			CsrMatrix csrA = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			var expectedB = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxationStrategy: "None"));

			var candidatesSelection = new ConstantCandidateSelection();
			Matrix computedB = candidatesSelection.ChooseCandidateVectors(csrA);

			var comparer = new MatrixComparer(tolerance: 1E-25);
			comparer.AssertEqual(expectedB, computedB);
		}
	}
}
