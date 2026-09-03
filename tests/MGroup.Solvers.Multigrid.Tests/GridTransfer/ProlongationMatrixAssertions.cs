namespace MGroup.Solvers.Multigrid.Tests.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.Multigrid.GridTransfer;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Matrices.Builders;

	using Xunit;

	public static class ProlongationMatrixAssertions
	{
		/// <summary>
		/// Each fine dof should depend on 2 ^ dim neighbors, or fewer
		/// </summary>
		/// <param name="prolongation"></param>
		/// <param name="dimension"></param>
		public static void FineDofsDependOnLimitedCoarseDofs(DokRowMajor prolongation, int dimension)
		{
			int max = (int)Math.Pow(2, dimension);

			var nnzPerRow = new int[prolongation.NumRows];
			foreach ((int row, int col, double value) in prolongation.EnumerateNonZeros())
			{
				nnzPerRow[row]++;
			}

			for (int i = 0; i < prolongation.NumRows; i++)
			{
				Assert.True(nnzPerRow[i] <= max, $"The fine dof {i} has too many neighbors");
			}
		}

		public static void ReproducesLinearField1D(DokRowMajor prolongation, GridNodeCounts gridNodes, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 1);

			// Node coordinates for [0, 1] domain
			Vector xf = Linspace(0, 1, gridNodes.NumNodesFinePerAxis[0]);
			Vector xc = Linspace(0, 1, gridNodes.NumNodesCoarsePerAxis[0]);

			// Linear field parameters
			double a = 2.137;
			double b = -0.483;

			// Fields at nodes
			Vector uc = xc.DoToAllEntries(x => a * x + b);
			Vector ufExpected = xf.DoToAllEntries(x => a * x + b);

			// Interpolated field
			Vector ufInterp = prolongation.MultiplyRight(uc, false);
			Assert.True(ufExpected.Equals(ufInterp, tolerance), "A linear field is not interpolated correctly");
		}

		public static void ReproducesLinearField2D(DokRowMajor prolongation, GridNodeCounts gridNodes, int dofsPerNode, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 2);
			int nfx = gridNodes.NumNodesFinePerAxis[0];
			int nfy = gridNodes.NumNodesFinePerAxis[1];
			int ncx = gridNodes.NumNodesCoarsePerAxis[0];
			int ncy = gridNodes.NumNodesCoarsePerAxis[1];

			// Node coordinates for [0, 1]^2 domain
			Vector xf = Linspace(0, 1, nfx);
			Vector xc = Linspace(0, 1, ncx);
			Vector yf = Linspace(0, 1, nfy);
			Vector yc = Linspace(0, 1, ncy);

			// Field over coarse grid
			var uc = Vector.CreateZero(ncx * ncy * dofsPerNode);
			for (int i = 0; i < ncx; i++)
			{
				for (int  j = 0; j < ncy; j++)
				{
					int nodeID = j * ncx + i;
					for (int d = 0; d < dofsPerNode; d++)
					{
						// Linear field parameters
						double a = 1.1 + 0.2 * d;
						double b = -0.9 + 0.1 * d;
						double c = 0.3 * d;

						int dofID = nodeID * dofsPerNode + d;
						uc[dofID] = a * xc[i] + b * yc[j] + c;
					}
				}
			}

			// Field over fine grid
			var ufExpected = Vector.CreateZero(nfx * nfy * dofsPerNode);
			for (int i = 0; i < nfx; i++)
			{
				for (int j = 0; j < nfy; j++)
				{
					int nodeID = j * nfx + i;
					for (int d = 0; d < dofsPerNode; d++)
					{
						// Linear field parameters
						double a = 1.1 + 0.2 * d;
						double b = -0.9 + 0.1 * d;
						double c = 0.3 * d;

						int dofID = nodeID * dofsPerNode + d;
						ufExpected[dofID] = a * xf[i] + b * yf[j] + c;
					}
				}
			}

			// Interpolated field
			Vector ufInterp = prolongation.MultiplyRight(uc, false);
			Assert.True(ufExpected.Equals(ufInterp, tolerance), "A linear field is not interpolated correctly");
		}

		public static void ReproducesLinearField3D(DokRowMajor prolongation, GridNodeCounts gridNodes, int dofsPerNode, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 3);
			int nfx = gridNodes.NumNodesFinePerAxis[0];
			int nfy = gridNodes.NumNodesFinePerAxis[1];
			int nfz = gridNodes.NumNodesFinePerAxis[2];
			int ncx = gridNodes.NumNodesCoarsePerAxis[0];
			int ncy = gridNodes.NumNodesCoarsePerAxis[1];
			int ncz = gridNodes.NumNodesCoarsePerAxis[2];

			// Node coordinates for [0, 1]^2 domain
			Vector xf = Linspace(0, 1, nfx);
			Vector xc = Linspace(0, 1, ncx);
			Vector yf = Linspace(0, 1, nfy);
			Vector yc = Linspace(0, 1, ncy);
			Vector zf = Linspace(0, 1, nfz);
			Vector zc = Linspace(0, 1, ncz);

			// Field over coarse grid
			var uc = Vector.CreateZero(ncx * ncy * ncz * dofsPerNode);
			for (int i = 0; i < ncx; i++)
			{
				for (int j = 0; j < ncy; j++)
				{
					for (int k = 0;  k < ncz; k++)
					{
						int nodeID = k * ncx * ncy + j * ncx + i;
						for (int dof = 0; dof < dofsPerNode; dof++)
						{
							// Linear field parameters
							double a = 0.8 + 0.1 * dof;
							double b = -0.6 + 0.05 * dof;
							double c = 0.3 - 0.07 * dof;
							double d = 0.2 * dof;

							int dofID = nodeID * dofsPerNode + dof;
							uc[dofID] = a * xc[i] + b * yc[j] + c * zc[k] + d;
						}
					}
				}
			}

			// Field over fine grid
			var ufExpected = Vector.CreateZero(nfx * nfy * nfz * dofsPerNode);
			for (int i = 0; i < nfx; i++)
			{
				for (int j = 0; j < nfy; j++)
				{
					for (int k = 0; k < nfz; k++)
					{
						int nodeID = k * nfx * nfy + j * nfx + i;
						for (int dof = 0; dof < dofsPerNode; dof++)
						{
							// Linear field parameters
							double a = 0.8 + 0.1 * dof;
							double b = -0.6 + 0.05 * dof;
							double c = 0.3 - 0.07 * dof;
							double d = 0.2 * dof;

							int dofID = nodeID * dofsPerNode + dof;
							ufExpected[dofID] = a * xf[i] + b * yf[j] + c * zf[k] + d;
						}
					}
				}
			}

			// Interpolated field
			Vector ufInterp = prolongation.MultiplyRight(uc, false);
			Assert.True(ufExpected.Equals(ufInterp, tolerance), "A linear field is not interpolated correctly");
		}

		public static void RowsSumToOne(DokRowMajor prolongation)
		{
			var ones = Vector.CreateWithValue(prolongation.NumColumns, 1.0);
			Vector rowSums = prolongation.MultiplyRight(ones, true);
			Vector rowSumsExpected = Vector.CreateWithValue(prolongation.NumRows, 1.0);
			Assert.True(rowSumsExpected.Equals(rowSums));
		}

		/// <summary>
		/// Asserts that fine nodes that coincide with coarse nodes have exactly one non zero entry, equal to 1.
		/// </summary>
		public static void WeightsAre1ForCoincidentNodes1D(DokRowMajor prolongation, GridNodeCounts gridNodes, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 1);
			int nf = gridNodes.NumNodesFinePerAxis[0];
			int nc = gridNodes.NumNodesCoarsePerAxis[0];
			int coarseningRatio = (nf - 1) / (nc - 1);
			Assert.True((nf - 1) % (nc - 1) == 0);

			for (int i = 0; i < nf; i++)
			{
				if (i % coarseningRatio != 0) // Fine node does not coincide with a coarse node.
				{
					continue;
				}

				int I = i / coarseningRatio; // coarse node index

				List<(int col, double val)> nonZeros = prolongation.EnumerateNonZerosOfRow(i).ToList();
				Assert.True(nonZeros.Count == 1, $"Row {i} must have exactly 1 nonzero.");
				Assert.True(nonZeros[0].col == I, $"The nonzero entry of row {i} must be at column {I}.");
				Assert.True(Math.Abs(nonZeros[0].val - 1.0) < tolerance, $"The nonzero entry of row {i} must be equal to 1.");
			}
		}

		/// <summary>
		/// Asserts that fine nodes that coincide with coarse nodes have exactly one non zero entry, equal to 1.
		/// </summary>
		public static void WeightsAre1ForCoincidentNodes2D(DokRowMajor prolongation, GridNodeCounts gridNodes, int dofsPerNode, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 2);
			int nfx = gridNodes.NumNodesFinePerAxis[0];
			int nfy = gridNodes.NumNodesFinePerAxis[1];
			int ncx = gridNodes.NumNodesCoarsePerAxis[0];
			int ncy = gridNodes.NumNodesCoarsePerAxis[1];

			int ratioX = (nfx - 1) / (ncx - 1);
			int ratioY = (nfy - 1) / (ncy - 1);
			Assert.True((nfx - 1) % (ncx - 1) == 0);
			Assert.True((nfy - 1) % (ncy - 1) == 0);

			for (int i = 0; i < nfx; i++)
			{
				for (int j = 0; j < nfy; j++)
				{
					if ((i % ratioX != 0) || (j % ratioY != 0) ) // Fine node does not coincide with a coarse node.
					{
						continue;
					}

					// Coarse node indices
					int I = i / ratioX;
					int J = j / ratioY;
					int fineNodeID = j * nfx + i;
					int coarseNodeID = J * ncx + I;

					for (int d = 0; d < dofsPerNode; d++)
					{
						int row = fineNodeID * dofsPerNode + d;
						int column = coarseNodeID * dofsPerNode + d;

						List<(int col, double val)> nonZeros = prolongation.EnumerateNonZerosOfRow(row).ToList();
						Assert.True(nonZeros.Count == 1, $"Row {i} must have exactly 1 nonzero.");
						Assert.True(nonZeros[0].col == column, $"The nonzero entry of row {i} must be at column {j}.");
						Assert.True(Math.Abs(nonZeros[0].val - 1.0) < tolerance, $"The nonzero entry of row {i} must be equal to 1.");
					}
				}
			}
		}

		/// <summary>
		/// Asserts that fine nodes that coincide with coarse nodes have exactly one non zero entry, equal to 1.
		/// </summary>
		public static void WeightsAre1ForCoincidentNodes3D(DokRowMajor prolongation, GridNodeCounts gridNodes, int dofsPerNode, double tolerance)
		{
			Assert.True(gridNodes.Dimension == 3);
			int nfx = gridNodes.NumNodesFinePerAxis[0];
			int nfy = gridNodes.NumNodesFinePerAxis[1];
			int nfz = gridNodes.NumNodesFinePerAxis[2];
			int ncx = gridNodes.NumNodesCoarsePerAxis[0];
			int ncy = gridNodes.NumNodesCoarsePerAxis[1];
			int ncz = gridNodes.NumNodesCoarsePerAxis[2];

			int ratioX = (nfx - 1) / (ncx - 1);
			int ratioY = (nfy - 1) / (ncy - 1);
			int ratioZ = (nfz - 1) / (ncz - 1);
			Assert.True((nfx - 1) % (ncx - 1) == 0);
			Assert.True((nfy - 1) % (ncy - 1) == 0);
			Assert.True((nfz - 1) % (ncz - 1) == 0);

			for (int i = 0; i < nfx; i++)
			{
				for (int j = 0; j < nfy; j++)
				{
					for (int k = 0; k < nfz; k++)
					{
						if ((i % ratioX != 0) || (j % ratioY != 0) || (k % ratioZ != 0)) // Fine node does not coincide with a coarse node.
						{
							continue;
						}

						// Coarse node indices
						int I = i / ratioX;
						int J = j / ratioY;
						int K = k / ratioZ;
						int fineNodeID = k * nfx * nfy + j * nfx + i;
						int coarseNodeID = K * ncx * ncy + J * ncx + I;

						for (int d = 0; d < dofsPerNode; d++)
						{
							int row = fineNodeID * dofsPerNode + d;
							int column = coarseNodeID * dofsPerNode + d;

							List<(int col, double val)> nonZeros = prolongation.EnumerateNonZerosOfRow(row).ToList();
							Assert.True(nonZeros.Count == 1, $"Row {i} must have exactly 1 nonzero.");
							Assert.True(nonZeros[0].col == column, $"The nonzero entry of row {i} must be at column {j}.");
							Assert.True(Math.Abs(nonZeros[0].val - 1.0) < tolerance, $"The nonzero entry of row {i} must be equal to 1.");
						}
					}
				}
			}
		}

		public static void WeightsAreInRange0to1(DokRowMajor prolongation)
		{
			foreach ((int row, int col, double value) in prolongation.EnumerateNonZeros())
			{
				Assert.True(value >= 0, $"P[{row}, {col}] < 0");
				Assert.True(value <= 1, $"P[{row}, {col}] > 1");
			}
		}

		private static Vector Linspace(double start, double end, int numPoints)
		{
			if (numPoints <= 0)
			{
				throw new ArgumentException();
			}

			if (numPoints == 1)
			{
				return Vector.CreateWithValue(1, start);
			}

			double dx = (end - start) / (numPoints - 1);
			var result = new double[numPoints];
			for (int i = 0; i < numPoints; i++)
			{
				result[i] = start + i * dx;
			}

			return Vector.CreateFromArray(result);
		}
	}
}
