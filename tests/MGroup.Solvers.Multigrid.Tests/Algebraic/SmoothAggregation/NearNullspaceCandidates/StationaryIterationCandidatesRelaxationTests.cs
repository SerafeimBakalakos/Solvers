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
		public static double[,] GetFineCandidatesPoisson1D(string relaxationStrategy)
		{
			if (relaxationStrategy == "None")
			{
				return new double[,]
				{
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 },
					{ 1.0 }
				};
			}
			else if (relaxationStrategy == "SGS-4")
			{
				return new double[,]
				{
					{ 0.1824138483343726 },
					{ 0.3648276966687452 },
					{ 0.5165402787220331 },
					{ 0.6288922203198463 },
					{ 0.6987160332233953 },
					{ 0.7255399806696738 },
					{ 0.7092479831569433 },
					{ 0.6490469913098891 },
					{ 0.5440050161211692 },
					{ 0.39517555515785957},
					{ 0.20892285858466839}
				};
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public static void TestSymmetricGaussSeidelForPoisson1D()
		{
			int numElements = 12;
			double theta = 0;
			(DokRowMajor dokA, _) = Poisson1DProblem.CreateWithConstantSource(numElements, source: 1.0);
			CsrMatrix csrA = dokA.BuildCsrMatrix(sortColsOfEachRow: true);
			AggregateCollection aggregates = StandardAggregationTests.GetAggregatesForPoisson1D(numElements, theta);

			var matrixB = Matrix.CreateFromArray(GetFineCandidatesPoisson1D(relaxationStrategy: "None"));

			var stationaryIteration = new SgsIterationCsr();
			var candidatesRelaxation = new StationaryIterationCandidatesRelaxation(stationaryIteration, 4);
			candidatesRelaxation.ImproveCandidateVectors(csrA, matrixB);

			var expectedB = Matrix.CreateFromArray(GetFineCandidatesPoisson1D(relaxationStrategy: "SGS-4"));
			var comparer = new MatrixComparer(tolerance: 1E-20);
			comparer.AssertEqual(expectedB, matrixB);
		}
	}
}
