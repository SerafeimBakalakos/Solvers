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
		[Fact]
		public static void TestForPoisson1DNoRelaxation()
		{
			int numElements = 12;
			double theta = 0;
			AggregateCollection aggregates = StandardAggregationTests.GetAggregatesForPoisson1D(numElements, theta);
			Assert.Equal(11, aggregates.AggregatesOfFineDofs.Length);
			Assert.Equal(4, aggregates.NumAggregates);

			var fineCandidates = Matrix.CreateWithValue(11, 1, 1.0);
			var candidateFitting = new CandidateVectorFitting(orthonormalizationTolerance: 1E-10);
			(CsrMatrix tentativeProlongation, Matrix coarseCandidates) = candidateFitting.FitCandidates(aggregates, fineCandidates);

			int expectedNumRows = 11;
			int expectedNumCols = 4;
			int[] expectedColIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] expectedRowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
			double[] expectedValues = [0.7071067811865475, 0.7071067811865475, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258];
			AssertUtilities.AssertEqualCsr(expectedNumCols, expectedValues, expectedColIndices, expectedRowOffsets, tentativeProlongation, tolerance: 1E-12);

			var expectedCoarseCandidates = Matrix.CreateFromArray(new double[,]
			{
				{ 1.4142135623730951 },
				{ 1.7320508075688772 },
				{ 1.7320508075688772 },
				{ 1.7320508075688772 } ,
			});
			var matrixComparer = new MatrixComparer(tolerance: 1E-12);
			matrixComparer.AssertEqual(expectedCoarseCandidates, coarseCandidates);
		}

		[Fact]
		public static void TestForPoisson1DRelaxation()
		{
			int numElements = 12;
			double theta = 0;
			AggregateCollection aggregates = StandardAggregationTests.GetAggregatesForPoisson1D(numElements, theta);
			Assert.Equal(11, aggregates.AggregatesOfFineDofs.Length);
			Assert.Equal(4, aggregates.NumAggregates);

			var fineCandidates = Matrix.CreateFromArray(new double[,]
			{
				{  0.1824138483343726 },
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
			});
			var candidateFitting = new CandidateVectorFitting(orthonormalizationTolerance: 1E-10);
			(CsrMatrix tentativeProlongation, Matrix coarseCandidates) = candidateFitting.FitCandidates(aggregates, fineCandidates);

			int expectedNumRows = 11;
			int expectedNumCols = 4;
			int[] expectedColIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
			int[] expectedRowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
			double[] expectedValues = [0.44721359549995787, 0.8944271909999157, 0.4815667224997867, 0.5863116156097816, 0.6514078455022331, 0.6023821583015316, 0.5888556692227306, 0.5388735808928088, 0.7726268882167728, 0.5612508165052404, 0.29672413548052906];
			AssertUtilities.AssertEqualCsr(expectedNumCols, expectedValues, expectedColIndices, expectedRowOffsets, tentativeProlongation, tolerance: 1E-12);

			var expectedCoarseCandidates = Matrix.CreateFromArray(new double[,]
			{
				{ 0.40788976491299395 },
				{ 1.0726245286233662 },
				{ 1.2044513116314672 },
				{ 0.7040979603709313 }
			});
			var matrixComparer = new MatrixComparer(tolerance: 1E-12);
			matrixComparer.AssertEqual(expectedCoarseCandidates, coarseCandidates);
		}
	}
}
