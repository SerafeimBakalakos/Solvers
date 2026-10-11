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
			int numElements = 12;
			double theta = 0;
			(DokRowMajor dokA, _) = Poisson1DProblem.CreateWithConstantSource(numElements, source: 1.0);
			CsrMatrix csrA = dokA.BuildCsrMatrix(sortColsOfEachRow: true);

			var candidatesSelection = new ConstantCandidateSelection();
			Matrix computedB = candidatesSelection.ChooseCandidateVectors(csrA);
			var expectedB = Matrix.CreateFromArray(StationaryIterationCandidatesRelaxationTests.GetFineCandidatesPoisson1D(relaxationStrategy: "None"));

			var comparer = new MatrixComparer(tolerance: 1E-25);
			comparer.AssertEqual(expectedB, computedB);
		}
	}
}
