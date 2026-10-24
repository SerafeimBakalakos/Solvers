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
		public static void TestSymmetricGaussSeidelForPoisson1D()
		{
			CsrMatrix matrixA = Poisson1D_12Elements_SaAmgResults.GetSystemMatrix(level: 0);
			var matrixB = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxationStrategy: "None"));

			var stationaryIteration = new SgsIterationCsr();
			var candidatesRelaxation = new StationaryIterationCandidatesRelaxation(stationaryIteration, 4);
			candidatesRelaxation.ImproveCandidateVectors(matrixA, matrixB);

			var expectedB = Matrix.CreateFromArray(Poisson1D_12Elements_SaAmgResults.GetNullspaceCandidates(level: 0, relaxationStrategy: "SGS-4"));
			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedB, matrixB);
		}
	}
}
