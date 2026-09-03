namespace MGroup.Solvers.Multigrid.Tests.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.GridTransfer.Geometric;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	using Xunit;

	public class Prolongation1DStrategyTests
	{
		public static TheoryData<GridNodeCounts> TestCases
			=> new TheoryData<GridNodeCounts>()
		{
			GridNodeCounts.Create1D(5, 3),
			GridNodeCounts.Create1D(7, 3),
		};

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestFineDofsDependOnLimitedCoarseDofs(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.FineDofsDependOnLimitedCoarseDofs(matrix, 1);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestReproducesLinearField1D(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.ReproducesLinearField1D(matrix, gridNodes, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestRowsSumToOne(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.RowsSumToOne(matrix);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAre1ForCoincidentNodes(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAre1ForCoincidentNodes1D(matrix, gridNodes, tol);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestWeightsAreInRange0to1(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			ProlongationMatrixAssertions.WeightsAreInRange0to1(matrix);
		}

		[Theory]
		[MemberData(nameof(TestCases))]
		public void TestExactMatrix(GridNodeCounts gridNodes)
		{
			double tol = 1E-12;
			var strategy = new Prolongation1DStrategy(tol);
			DokRowMajor matrix = strategy.CreateProlongationMatrix(gridNodes.NumNodesFinePerAxis, gridNodes.NumNodesCoarsePerAxis);
			var matrixExpected = Matrix.CreateFromArray(GetExpectedProlongation(gridNodes));
			Assert.True(matrix.Equals(matrixExpected, tol));
		}

		private static double[,] GetExpectedProlongation(GridNodeCounts gridNodes)
		{
			if (gridNodes.Equals(GridNodeCounts.Create1D(5, 3)))
			{
				return new double[,]
				{
					{ 1, 0, 0 },
					{ 0.5, 0.5, 0 },
					{ 0, 1, 0 },
					{ 0, 0.5, 0.5 },
					{ 0, 0, 1 }
				};
			}
			else if (gridNodes.Equals(GridNodeCounts.Create1D(7, 3)))
			{
				return new double[,]
				{
					{ 1, 0, 0 },
					{ 2.0/3, 1.0/3, 0 },
					{ 1.0/3, 2.0/3, 0 },
					{ 0, 1, 0 },
					{ 0, 2.0/3, 1.0/3 },
					{ 0, 1.0/3, 2.0/3 },
					{ 0, 0, 1 }
				};
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
