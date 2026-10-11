namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class CandidateVectorFittingTests
	{
		public static double[,] GetCoarseCandidatesPoisson1D(string relaxationStrategy)
		{
			if (relaxationStrategy == "None")
			{
				return new double[,]
				{
					{ 1.4142135623730951 },
					{ 1.7320508075688772 },
					{ 1.7320508075688772 },
					{ 1.7320508075688772 }
				};
			}
			else if (relaxationStrategy == "SGS-4")
			{
				return new double[,]
				{
					{ 0.40788976491299395 },
					{ 1.0726245286233662 },
					{ 1.2044513116314672 },
					{ 0.7040979603709313 }
				};
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public static CsrMatrix GetTentativeProlongationPoisson1D(string relaxationStrategy)
		{
			if (relaxationStrategy == "None")
			{
				double[] values = [0.7071067811865475, 0.7071067811865475, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258];
				int[] colIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
				int[] rowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
				return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
			}
			else if (relaxationStrategy == "SGS-4")
			{
				double[] values = [0.44721359549995787, 0.8944271909999157, 0.4815667224997867, 0.5863116156097816, 0.6514078455022331, 0.6023821583015316, 0.5888556692227306, 0.5388735808928088, 0.7726268882167728, 0.5612508165052404, 0.29672413548052906];
				int[] colIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
				int[] rowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
				return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		[Theory]
		[InlineData("None")]
		[InlineData("SGS-4")]
		public static void TestForPoisson1DRelaxation(string relaxation)
		{
			int numElements = 12;
			double theta = 0;
			AggregateCollection aggregates = StandardAggregationTests.GetAggregatesForPoisson1D(numElements, theta);
			Assert.Equal(11, aggregates.AggregatesOfFineDofs.Length);
			Assert.Equal(4, aggregates.NumAggregates);

			var fineCandidates = Matrix.CreateFromArray(StationaryIterationCandidatesRelaxationTests.GetFineCandidatesPoisson1D(relaxation));
			var candidateFitting = new CandidateVectorFitting(orthonormalizationTolerance: 1E-10);
			(CsrMatrix tentativeProlongation, Matrix coarseCandidates) = candidateFitting.FitCandidates(aggregates, fineCandidates);

			CsrMatrix expectedPt = GetTentativeProlongationPoisson1D(relaxation);
			AssertUtilities.AssertEqualCsr(expectedPt, tentativeProlongation, tolerance: 1E-20);

			var expectedCoarseCandidates = Matrix.CreateFromArray(GetCoarseCandidatesPoisson1D(relaxation));
			var matrixComparer = new MatrixComparer(tolerance: 1E-20);
			matrixComparer.AssertEqual(expectedCoarseCandidates, coarseCandidates);
		}
	}
}
