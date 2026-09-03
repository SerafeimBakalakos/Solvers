namespace MGroup.Solvers.Multigrid.Tests.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	using Xunit;

	public class Prolongation2DScalarStrategyTests
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
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.FineDofsDependOnLimitedCoarseDofs(matrix, dimension:2);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestReproducesLinearField2D(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.ReproducesLinearField2D(matrix, gridNodes, dofsPerNode:1, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestRowsSumToOne(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.RowsSumToOne(matrix);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAre1ForCoincidentNodes(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAre1ForCoincidentNodes2D(matrix, gridNodes, dofsPerNode:1, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAreInRange0to1(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAreInRange0to1(matrix);
		}

		[Fact]
		public void TestExactMatrix()
		{
			var gridNodes = GridNodeCounts.Create2D(5, 10, 3, 4);
			double tol = 1E-12;
			var strategy = new Prolongation2DScalarStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);

			var matrixExpected = DokRowMajor.CreateEmpty(50, 12);
			matrixExpected[0, 0] = 1.000;
			matrixExpected[1, 0] = 0.500;
			matrixExpected[5, 0] = 2.0 / 3;
			matrixExpected[6, 0] = 1.0 / 3;
			matrixExpected[10, 0] = 1.0 / 3;
			matrixExpected[11, 0] = 1.0 / 6;
			matrixExpected[1, 1] = 0.500;
			matrixExpected[2, 1] = 1.000;
			matrixExpected[3, 1] = 0.500;
			matrixExpected[6, 1] = 1.0 / 3;
			matrixExpected[7, 1] = 2.0 / 3;
			matrixExpected[8, 1] = 1.0 / 3;
			matrixExpected[11, 1] = 1.0 / 6;
			matrixExpected[12, 1] = 1.0 / 3;
			matrixExpected[13, 1] = 1.0 / 6;
			matrixExpected[3,  2] = 0.500;
			matrixExpected[4,  2] = 1.000;
			matrixExpected[8,  2] = 1.0 / 3;
			matrixExpected[9,  2] = 2.0 / 3;
			matrixExpected[13, 2] = 1.0 / 6;
			matrixExpected[14, 2] = 1.0 / 3;
			matrixExpected[5,  3] = 1.0 / 3;
			matrixExpected[6,  3] = 1.0 / 6;
			matrixExpected[10, 3] = 2.0 / 3;
			matrixExpected[11, 3] = 1.0 / 3;
			matrixExpected[15, 3] = 1.000;
			matrixExpected[16, 3] = 0.500;
			matrixExpected[20, 3] = 2.0 / 3;
			matrixExpected[21, 3] = 1.0 / 3;
			matrixExpected[25, 3] = 1.0 / 3;
			matrixExpected[26, 3] = 1.0 / 6;
			matrixExpected[6,  4] = 1.0 / 6;
			matrixExpected[7,  4] = 1.0 / 3;
			matrixExpected[8,  4] = 1.0 / 6;
			matrixExpected[11, 4] = 1.0 / 3;
			matrixExpected[12, 4] = 2.0 / 3;
			matrixExpected[13, 4] = 1.0 / 3;
			matrixExpected[16, 4] = 0.500;
			matrixExpected[17, 4] = 1.000;
			matrixExpected[18, 4] = 0.500;
			matrixExpected[21, 4] = 1.0 / 3;
			matrixExpected[22, 4] = 2.0 / 3;
			matrixExpected[23, 4] = 1.0 / 3;
			matrixExpected[26, 4] = 1.0 / 6;
			matrixExpected[27, 4] = 1.0 / 3;
			matrixExpected[28, 4] = 1.0 / 6;
			matrixExpected[8,  5] = 1.0 / 6;
			matrixExpected[9,  5] = 1.0 / 3;
			matrixExpected[13, 5] = 1.0 / 3;
			matrixExpected[14, 5] = 2.0 / 3;
			matrixExpected[18, 5] = 0.500;
			matrixExpected[19, 5] = 1.000;
			matrixExpected[23, 5] = 1.0 / 3;
			matrixExpected[24, 5] = 2.0 / 3;
			matrixExpected[28, 5] = 1.0 / 6;
			matrixExpected[29, 5] = 1.0 / 3;
			matrixExpected[20, 6] = 1.0 / 3;
			matrixExpected[21, 6] = 1.0 / 6;
			matrixExpected[25, 6] = 2.0 / 3;
			matrixExpected[26, 6] = 1.0 / 3;
			matrixExpected[30, 6] = 1.000;
			matrixExpected[31, 6] = 0.500;
			matrixExpected[35, 6] = 2.0 / 3;
			matrixExpected[36, 6] = 1.0 / 3;
			matrixExpected[40, 6] = 1.0 / 3;
			matrixExpected[41, 6] = 1.0 / 6;
			matrixExpected[21, 7] = 1.0 / 6;
			matrixExpected[22, 7] = 1.0 / 3;
			matrixExpected[23, 7] = 1.0 / 6;
			matrixExpected[26, 7] = 1.0 / 3;
			matrixExpected[27, 7] = 2.0 / 3;
			matrixExpected[28, 7] = 1.0 / 3;
			matrixExpected[31, 7] = 0.500;
			matrixExpected[32, 7] = 1.000;
			matrixExpected[33, 7] = 0.500;
			matrixExpected[36, 7] = 1.0 / 3;
			matrixExpected[37, 7] = 2.0 / 3;
			matrixExpected[38, 7] = 1.0 / 3;
			matrixExpected[41, 7] = 1.0 / 6;
			matrixExpected[42, 7] = 1.0 / 3;
			matrixExpected[43, 7] = 1.0 / 6;
			matrixExpected[23, 8] = 1.0 / 6;
			matrixExpected[24, 8] = 1.0 / 3;
			matrixExpected[28, 8] = 1.0 / 3;
			matrixExpected[29, 8] = 2.0 / 3;
			matrixExpected[33, 8] = 0.500;
			matrixExpected[34, 8] = 1.000;
			matrixExpected[38, 8] = 1.0 / 3;
			matrixExpected[39, 8] = 2.0 / 3;
			matrixExpected[43, 8] = 1.0 / 6;
			matrixExpected[44, 8] = 1.0 / 3;
			matrixExpected[35, 9] = 1.0 / 3;
			matrixExpected[36, 9] = 1.0 / 6;
			matrixExpected[40, 9] = 2.0 / 3;
			matrixExpected[41, 9] = 1.0 / 3;
			matrixExpected[45, 9] = 1.000;
			matrixExpected[46, 9] = 0.500;
			matrixExpected[36, 10] = 1.0 / 6;
			matrixExpected[37, 10] = 1.0 / 3;
			matrixExpected[38, 10] = 1.0 / 6;
			matrixExpected[41, 10] = 1.0 / 3;
			matrixExpected[42, 10] = 2.0 / 3;
			matrixExpected[43, 10] = 1.0 / 3;
			matrixExpected[46, 10] = 0.500;
			matrixExpected[47, 10] = 1.000;
			matrixExpected[48, 10] = 0.500;
			matrixExpected[38, 11] = 1.0 / 6;
			matrixExpected[39, 11] = 1.0 / 3;
			matrixExpected[43, 11] = 1.0 / 3;
			matrixExpected[44, 11] = 2.0 / 3;
			matrixExpected[48, 11] = 0.500;
			matrixExpected[49, 11] = 1.000;

			Assert.True(matrix.Equals(matrixExpected, tol));
		}
	}
}
