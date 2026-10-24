namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class StationaryIterationCandidatesRelaxationTests
	{
		[Fact]
		public static void TestImproveCandidateVectors_Cantilever8x4Elements_SGS4()
		{
			// Level 0
			CsrMatrix A0 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 0);
			var B0 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(0));

			var stationaryIteration0 = new SgsIterationCsr();
			var candidatesRelaxation0 = new StationaryIterationCandidatesRelaxation(stationaryIteration0, 4);
			candidatesRelaxation0.ImproveCandidateVectors(A0, B0);

			var expectedB0 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(level: 0));
			var comparer0 = new MatrixComparer(tolerance: 1E-30);
			comparer0.AssertEqual(expectedB0, B0);

			// Level 1
			CsrMatrix A1 = Cantilever2D_8x4Elements_SaAmgResults.GetSystemMatrix(level: 1);
			var B1 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesInitial(1));

			var stationaryIteration1 = new SgsIterationCsr();
			var candidatesRelaxation1 = new StationaryIterationCandidatesRelaxation(stationaryIteration1, 4);
			candidatesRelaxation1.ImproveCandidateVectors(A1, B1);

			var expectedB1 = Matrix.CreateFromArray(Cantilever2D_8x4Elements_SaAmgResults.GetNullspaceCandidatesFinal(level: 1));
			var comparer1 = new MatrixComparer(tolerance: 1E-14);
			comparer1.AssertEqual(expectedB1, B1);
		}

		[Fact]
		public static void TestImproveCandidateVectors_Poisson12Elements_SGS4()
		{
			CsrMatrix matrixA = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			var matrixB = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxationStrategy: "None"));

			var stationaryIteration = new SgsIterationCsr();
			var candidatesRelaxation = new StationaryIterationCandidatesRelaxation(stationaryIteration, 4);
			candidatesRelaxation.ImproveCandidateVectors(matrixA, matrixB);

			var expectedB = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxationStrategy: "SGS-4"));
			var comparer = new MatrixComparer(tolerance: 1E-30);
			comparer.AssertEqual(expectedB, matrixB);
		}
	}
}
