namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Iterative.Stationary
{
	using System;

	using MGroup.LinearAlgebra.Matrices;
	//using MGroup.LinearAlgebra.Tests.TestData;
	//using MGroup.LinearAlgebra.Tests.Utilities;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public class StationaryIterationTests
	{
		[Fact]
		private static void TestGaussSeidelBackIteration()
		{
			var stationaryIteration = new GaussSeidelIterationCsr(forwardDirection: false);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// (U+D)*x(t+1) = b - L*x(t)
				SparseMatrix L = decomp.GetL();
				SparseMatrix UplusD = decomp.GetCombination("U+D", 0, 1, 1);
				return UplusD.SolveBackSubstitution(rhs - L * solution);
			});
		}

		[Fact]
		private static void TestGaussSeidelForwardIteration()
		{
			var stationaryIteration = new GaussSeidelIterationCsr(forwardDirection: true);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// (L+D)*x(t+1) = b - U*x(t)
				SparseMatrix U = decomp.GetU();
				SparseMatrix LplusD = decomp.GetCombination("L+D", 1, 1, 0);
				return LplusD.SolveForwardSubstitution(rhs - U * solution);
			});
		}

		[Fact]
		private static void TestGaussSeidelSymmetricIteration()
		{
			var stationaryIteration = new SgsIterationCsr();
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// Step 1: (D+L) * x(t+1/2) = b - U*x(t)
				// Step 2: (D+U) * x(t+1) = (b - L*x(t+1/2))
				SparseMatrix L = decomp.GetL();
				SparseMatrix U = decomp.GetU();
				SparseMatrix D = decomp.GetD();
				SparseMatrix DplusL = decomp.GetCombination("D+L", 1, 1, 0);
				SparseMatrix DplusU = decomp.GetCombination("D+U", 0, 1, 1);
				solution = DplusL.SolveForwardSubstitution(rhs - U * solution);
				return DplusU.SolveBackSubstitution(rhs - L * solution);
			});
		}

		[Fact]
		private static void TestJacobiIteration()
		{
			var stationaryIteration = new JacobiIterationCsr();
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// D*x(t+1) = b - (U+L)*x(t)
				SparseMatrix D = decomp.GetD();
				SparseMatrix LplusU = decomp.GetCombination("L+U", 1, 0, 1);
				return D.SolveDiagonal(rhs - LplusU * solution);
			});
		}

		[Fact]
		private static void TestJacobiWeightedIteration()
		{
			double omega = 1.2;
			var stationaryIteration = new WeightedJacobiIterationCsr(omega);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// Step 1: D*xJ = b - (U+L)*x(t)
				// Step 2: x(t+1) = ω*xJ + (1-ω)*x(t)
				SparseMatrix D = decomp.GetD();
				SparseMatrix LplusU = decomp.GetCombination("L+U", 1, 0, 1);
				Vector xJ = D.SolveDiagonal(rhs - LplusU * solution);
				return (1 - omega) * solution + omega * xJ;
			}, entrywiseTolerance: 1E-14);
		}

		[Fact]
		private static void TestSorBackIteration()
		{
			double omega = 1.2;
			var stationaryIteration = new SorIterationCsr(omega, forwardDirection: false);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// (D + ωU) * x(t + 1) = ω * (b - L * x(t)) + (1 - ω)*D*x(t)
				SparseMatrix L = decomp.GetL();
				SparseMatrix D = decomp.GetD();
				SparseMatrix lhsMatrix = decomp.GetCombination("D+ωU", 0, 1, omega);
				return lhsMatrix.SolveBackSubstitution(omega * (rhs - L * solution) + (1 - omega) * (D * solution));
			});
		}

		[Fact]
		private static void TestSorForwardIteration()
		{
			double omega = 1.2;
			var stationaryIteration = new SorIterationCsr(omega, forwardDirection: true);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// (D + ωL) * x(t + 1) = ω * (b - U * x(t)) + (1 - ω)*D*x(t)
				SparseMatrix U = decomp.GetU();
				SparseMatrix D = decomp.GetD();
				SparseMatrix lhsMatrix = decomp.GetCombination("D+ωL", omega, 1, 0);
				return lhsMatrix.SolveForwardSubstitution(omega * (rhs - U * solution) + (1 - omega) * (D * solution));
			});
		}

		[Fact]
		private static void TestSsorIteration()
		{
			double omega = 1.2;
			var stationaryIteration = new SsorIterationCsr(omega);
			RunMultipleApplications(4, stationaryIteration, (StationaryAlgorithmDecomposition decomp, Vector rhs, Vector solution) =>
			{
				// Step 1: (D+ωL) * x(t+1/2) = ω*(b -U*x(t)) +(1-ω)D*x(t)
				// Step 2: (D+ωU) * x(t+1) = ω*(b -L*x(t+1/2)) +(1-ω)D*x(t+1/2)
				SparseMatrix L = decomp.GetL();
				SparseMatrix U = decomp.GetU();
				SparseMatrix D = decomp.GetD();
				SparseMatrix DplusWL = decomp.GetCombination("D+ωL", omega, 1, 0);
				SparseMatrix DplusWU = decomp.GetCombination("D+ωU", 0, 1, omega);
				solution = DplusWL.SolveForwardSubstitution(omega * (rhs - U * solution) + (1 - omega) * (D * solution));
				return DplusWU.SolveBackSubstitution(omega * (rhs - L * solution) + (1 - omega) * (D * solution));
			});
		}

		private static void RunMultipleApplications(int numApplications, IStationaryIteration stationaryIteration,
			Func<StationaryAlgorithmDecomposition, Vector, Vector, Vector> applyIterationUsingMatrixForm, double entrywiseTolerance = 1E-15)
		{
			// Setup comparison code
			var comparer = new MatrixComparer(entrywiseTolerance);

			// Initialize matrices and vectors
			var csrMatrix = CsrMatrix.CreateFromArrays(SparsePosDef10by10.Order, SparsePosDef10by10.Order,
					SparsePosDef10by10.CsrValues, SparsePosDef10by10.CsrColIndices, SparsePosDef10by10.CsrRowOffsets, true);
			var b = Vector.CreateFromArray(SparsePosDef10by10.Rhs);
			var xExpected = Vector.CreateZero(b.Length);
			var xComputed = Vector.CreateZero(b.Length);

			// Prepare stationary iteration
			var decomp = new StationaryAlgorithmDecomposition(SparseMatrix.CreateFromMatrix(csrMatrix));
			stationaryIteration.UpdateMatrix(csrMatrix, true);

			// Applications
			for (int i = 0; i < numApplications; i++)
			{
				xExpected = applyIterationUsingMatrixForm(decomp, b, xExpected);
				stationaryIteration.Execute(b, xComputed);
				comparer.AssertEqual(xExpected, xComputed);
			}
		}
	}
}
