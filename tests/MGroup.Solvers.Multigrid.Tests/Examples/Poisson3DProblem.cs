namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class Poisson3DProblem
	{
		private static readonly int[] LocalDx = { 0, 1, 1, 0, 0, 1, 1, 0 };
		private static readonly int[] LocalDy = { 0, 0, 1, 1, 0, 0, 1, 1 };
		private static readonly int[] LocalDz = { 0, 0, 0, 0, 1, 1, 1, 1 };

		private static readonly int[] Sx = { -1, 1, 1, -1, -1, 1, 1, -1 };
		private static readonly int[] Sy = { -1, -1, 1, 1, -1, -1, 1, 1 };
		private static readonly int[] Sz = { -1, -1, -1, -1, 1, 1, 1, 1 };

		public static (DokRowMajor A, Vector b) CreateLinearSystem(int numElementsX, int numElementsY, int numElementsZ, double constantSourceFunction)
		{
			if (numElementsX < 1 || numElementsY < 1 || numElementsZ < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(numElementsX), "The number of elements in each direction must be positive.");
			}

			double hx = 1.0 / numElementsX;
			double hy = 1.0 / numElementsY;
			double hz = 1.0 / numElementsZ;

			int nx = numElementsX - 1;
			int ny = numElementsY - 1;
			int nz = numElementsZ - 1;
			int numUnknowns = nx * ny * nz;

			var A = DokRowMajor.CreateEmpty(numUnknowns, numUnknowns);
			var rhs = new double[numUnknowns];

			double[,] localStiffness = CreateLocalStiffnessMatrix(hx, hy, hz);
			double localLoad = constantSourceFunction * hx * hy * hz / 8.0;

			for (int k = 0; k < numElementsZ; k++)
			{
				for (int j = 0; j < numElementsY; j++)
				{
					for (int i = 0; i < numElementsX; i++)
					{
						var dofs = new int[8];

						for (int a = 0; a < 8; a++)
						{
							dofs[a] = GetInteriorDof(i + LocalDx[a], j + LocalDy[a], k + LocalDz[a], numElementsX, numElementsY, numElementsZ);
						}

						for (int a = 0; a < 8; a++)
						{
							if (dofs[a] >= 0)
							{
								rhs[dofs[a]] += localLoad;
							}
						}

						AddElementStiffness(A, dofs, localStiffness);
					}
				}
			}

			return (A, Vector.CreateFromArray(rhs));
		}

		private static double[,] CreateLocalStiffnessMatrix(double hx, double hy, double hz)
		{
			var localStiffness = new double[8, 8];
			double[] gaussPoints = { -1.0 / Math.Sqrt(3.0), 1.0 / Math.Sqrt(3.0) };
			double jacobianDeterminant = hx * hy * hz / 8.0;

			foreach (double xi in gaussPoints)
			{
				foreach (double eta in gaussPoints)
				{
					foreach (double zeta in gaussPoints)
					{
						var dNdx = new double[8];
						var dNdy = new double[8];
						var dNdz = new double[8];

						for (int a = 0; a < 8; a++)
						{
							double dNdxi = Sx[a] * (1.0 + Sy[a] * eta) * (1.0 + Sz[a] * zeta) / 8.0;
							double dNdeta = Sy[a] * (1.0 + Sx[a] * xi) * (1.0 + Sz[a] * zeta) / 8.0;
							double dNdzeta = Sz[a] * (1.0 + Sx[a] * xi) * (1.0 + Sy[a] * eta) / 8.0;

							dNdx[a] = dNdxi * (2.0 / hx);
							dNdy[a] = dNdeta * (2.0 / hy);
							dNdz[a] = dNdzeta * (2.0 / hz);
						}

						for (int a = 0; a < 8; a++)
						{
							for (int b = 0; b < 8; b++)
							{
								localStiffness[a, b] += jacobianDeterminant * (
									dNdx[a] * dNdx[b]
									+ dNdy[a] * dNdy[b]
									+ dNdz[a] * dNdz[b]);
							}
						}
					}
				}
			}

			return localStiffness;
		}

		private static void AddElementStiffness(DokRowMajor A, int[] dofs, double[,] localStiffness)
		{
			for (int a = 0; a < 8; a++)
			{
				for (int b = a + 1; b < 8; b++)
				{
					double value = -localStiffness[a, b];
					int dofA = dofs[a];
					int dofB = dofs[b];

					if (dofA >= 0)
					{
						A.AddToEntry(dofA, dofA, value);
					}

					if (dofB >= 0)
					{
						A.AddToEntry(dofB, dofB, value);
					}

					if (dofA >= 0 && dofB >= 0)
					{
						A.AddToEntry(dofA, dofB, -value);
						A.AddToEntry(dofB, dofA, -value);
					}
				}
			}
		}

		private static int GetInteriorDof(int i, int j, int k, int numElementsX, int numElementsY, int numElementsZ)
		{
			if (i <= 0 || i >= numElementsX || j <= 0 || j >= numElementsY || k <= 0 || k >= numElementsZ)
			{
				return -1;
			}

			int nx = numElementsX - 1;
			int ny = numElementsY - 1;

			return (k - 1) * nx * ny + (j - 1) * nx + (i - 1);
		}
	}
}
