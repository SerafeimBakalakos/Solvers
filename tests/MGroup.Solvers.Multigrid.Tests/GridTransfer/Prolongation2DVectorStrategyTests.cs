namespace MGroup.Solvers.Multigrid.Tests.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	using Xunit;

	public class Prolongation2DVectorStrategyTests
	{
		public static TheoryData<GridNodeCounts> TestCases
			=> new TheoryData<GridNodeCounts>()
		{
			GridNodeCounts.Create2D(7, 5, 4, 3), // isotropic
			GridNodeCounts.Create2D(5, 7, 3, 4), // isotropic
			GridNodeCounts.Create2D(10, 5, 4, 3), // anisotropic
			GridNodeCounts.Create2D(5, 10, 3, 4), // anisotropic
			GridNodeCounts.Create2D(13, 5, 7, 5), // no coarsening along y
			GridNodeCounts.Create2D(7, 5, 7, 3), // no coarsening along x
		};

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestFineDofsDependOnLimitedCoarseDofs(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.FineDofsDependOnLimitedCoarseDofs(matrix, dimension:2);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestReproducesLinearField2D(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.ReproducesLinearField2D(matrix, gridNodes, dofsPerNode:2, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestRowsSumToOne(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.RowsSumToOne(matrix);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAre1ForCoincidentNodes(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAre1ForCoincidentNodes2D(matrix, gridNodes, dofsPerNode:2, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAreInRange0to1(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAreInRange0to1(matrix);
		}

		[Fact]
		public void TestExactMatrix()
		{
			var gridNodes = GridNodeCounts.Create2D(5, 7, 3, 4);
			double tol = 1E-12;
			var strategy = new Prolongation2DVectorStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);

			var matrixExpected = DokRowMajor.CreateEmpty(70, 24);
			matrixExpected[0, 0] = 1.00;
			matrixExpected[2, 0] = 0.50;
			matrixExpected[10, 0] = 0.50;
			matrixExpected[12, 0] = 0.25;
			matrixExpected[1, 1] = 1.00;
			matrixExpected[3, 1] = 0.50;
			matrixExpected[11, 1] = 0.50;
			matrixExpected[13, 1] = 0.25;
			matrixExpected[2, 2] = 0.50;
			matrixExpected[4, 2] = 1.00;
			matrixExpected[6, 2] = 0.50;
			matrixExpected[12, 2] = 0.25;
			matrixExpected[14, 2] = 0.50;
			matrixExpected[16, 2] = 0.25;
			matrixExpected[3, 3] = 0.50;
			matrixExpected[5, 3] = 1.00;
			matrixExpected[7, 3] = 0.50;
			matrixExpected[13, 3] = 0.25;
			matrixExpected[15, 3] = 0.50;
			matrixExpected[17, 3] = 0.25;
			matrixExpected[6, 4] = 0.50;
			matrixExpected[8, 4] = 1.00;
			matrixExpected[16, 4] = 0.25;
			matrixExpected[18, 4] = 0.50;
			matrixExpected[7, 5] = 0.50;
			matrixExpected[9, 5] = 1.00;
			matrixExpected[17, 5] = 0.25;
			matrixExpected[19, 5] = 0.50;
			matrixExpected[10, 6] = 0.50;
			matrixExpected[12, 6] = 0.25;
			matrixExpected[20, 6] = 1.00;
			matrixExpected[22, 6] = 0.50;
			matrixExpected[30, 6] = 0.50;
			matrixExpected[32, 6] = 0.25;
			matrixExpected[11, 7] = 0.50;
			matrixExpected[13, 7] = 0.25;
			matrixExpected[21, 7] = 1.00;
			matrixExpected[23, 7] = 0.50;
			matrixExpected[31, 7] = 0.50;
			matrixExpected[33, 7] = 0.25;
			matrixExpected[12, 8] = 0.25;
			matrixExpected[14, 8] = 0.50;
			matrixExpected[16, 8] = 0.25;
			matrixExpected[22, 8] = 0.50;
			matrixExpected[24, 8] = 1.00;
			matrixExpected[26, 8] = 0.50;
			matrixExpected[32, 8] = 0.25;
			matrixExpected[34, 8] = 0.50;
			matrixExpected[36, 8] = 0.25;
			matrixExpected[13, 9] = 0.25;
			matrixExpected[15, 9] = 0.50;
			matrixExpected[17, 9] = 0.25;
			matrixExpected[23, 9] = 0.50;
			matrixExpected[25, 9] = 1.00;
			matrixExpected[27, 9] = 0.50;
			matrixExpected[33, 9] = 0.25;
			matrixExpected[35, 9] = 0.50;
			matrixExpected[37, 9] = 0.25;
			matrixExpected[16, 10] = 0.25;
			matrixExpected[18, 10] = 0.50;
			matrixExpected[26, 10] = 0.50;
			matrixExpected[28, 10] = 1.00;
			matrixExpected[36, 10] = 0.25;
			matrixExpected[38, 10] = 0.50;
			matrixExpected[17, 11] = 0.25;
			matrixExpected[19, 11] = 0.50;
			matrixExpected[27, 11] = 0.50;
			matrixExpected[29, 11] = 1.00;
			matrixExpected[37, 11] = 0.25;
			matrixExpected[39, 11] = 0.50;
			matrixExpected[30, 12] = 0.50;
			matrixExpected[32, 12] = 0.25;
			matrixExpected[40, 12] = 1.00;
			matrixExpected[42, 12] = 0.50;
			matrixExpected[50, 12] = 0.50;
			matrixExpected[52, 12] = 0.25;
			matrixExpected[31, 13] = 0.50;
			matrixExpected[33, 13] = 0.25;
			matrixExpected[41, 13] = 1.00;
			matrixExpected[43, 13] = 0.50;
			matrixExpected[51, 13] = 0.50;
			matrixExpected[53, 13] = 0.25;
			matrixExpected[32, 14] = 0.25;
			matrixExpected[34, 14] = 0.50;
			matrixExpected[36, 14] = 0.25;
			matrixExpected[42, 14] = 0.50;
			matrixExpected[44, 14] = 1.00;
			matrixExpected[46, 14] = 0.50;
			matrixExpected[52, 14] = 0.25;
			matrixExpected[54, 14] = 0.50;
			matrixExpected[56, 14] = 0.25;
			matrixExpected[33, 15] = 0.25;
			matrixExpected[35, 15] = 0.50;
			matrixExpected[37, 15] = 0.25;
			matrixExpected[43, 15] = 0.50;
			matrixExpected[45, 15] = 1.00;
			matrixExpected[47, 15] = 0.50;
			matrixExpected[53, 15] = 0.25;
			matrixExpected[55, 15] = 0.50;
			matrixExpected[57, 15] = 0.25;
			matrixExpected[36, 16] = 0.25;
			matrixExpected[38, 16] = 0.50;
			matrixExpected[46, 16] = 0.50;
			matrixExpected[48, 16] = 1.00;
			matrixExpected[56, 16] = 0.25;
			matrixExpected[58, 16] = 0.50;
			matrixExpected[37, 17] = 0.25;
			matrixExpected[39, 17] = 0.50;
			matrixExpected[47, 17] = 0.50;
			matrixExpected[49, 17] = 1.00;
			matrixExpected[57, 17] = 0.25;
			matrixExpected[59, 17] = 0.50;
			matrixExpected[50, 18] = 0.50;
			matrixExpected[52, 18] = 0.25;
			matrixExpected[60, 18] = 1.00;
			matrixExpected[62, 18] = 0.50;
			matrixExpected[51, 19] = 0.50;
			matrixExpected[53, 19] = 0.25;
			matrixExpected[61, 19] = 1.00;
			matrixExpected[63, 19] = 0.50;
			matrixExpected[52, 20] = 0.25;
			matrixExpected[54, 20] = 0.50;
			matrixExpected[56, 20] = 0.25;
			matrixExpected[62, 20] = 0.50;
			matrixExpected[64, 20] = 1.00;
			matrixExpected[66, 20] = 0.50;
			matrixExpected[53, 21] = 0.25;
			matrixExpected[55, 21] = 0.50;
			matrixExpected[57, 21] = 0.25;
			matrixExpected[63, 21] = 0.50;
			matrixExpected[65, 21] = 1.00;
			matrixExpected[67, 21] = 0.50;
			matrixExpected[56, 22] = 0.25;
			matrixExpected[58, 22] = 0.50;
			matrixExpected[66, 22] = 0.50;
			matrixExpected[68, 22] = 1.00;
			matrixExpected[57, 23] = 0.25;
			matrixExpected[59, 23] = 0.50;
			matrixExpected[67, 23] = 0.50;
			matrixExpected[69, 23] = 1.00;

			Assert.True(matrix.Equals(matrixExpected, tol));
		}
	}
}
