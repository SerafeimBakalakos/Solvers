namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class Cantilever3DElasticityProblem
	{
		public const double LengthX = 3.0;
		public const double LengthY = 1.0;
		public const double LengthZ = 2.0;

		private const double TotalLoadY = 4000.0;
		private const double TotalLoadZ = 3000.0;
		private const double ElasticityModulus = 9.0e5;
		private const double PoissonRatio = 0.3;

		// Local node order:
		// z = 0: 0 -------- 1
		//        |          |
		//        |          |
		//        3 -------- 2
		//
		// z = Lz: 4 -------- 5
		//         |          |
		//         |          |
		//         7 -------- 6
		//
		// Local DOFs: [ux0, uy0, uz0, ..., ux7, uy7, uz7].
		private static readonly int[] LocalDx = { 0, 1, 1, 0, 0, 1, 1, 0 };
		private static readonly int[] LocalDy = { 0, 0, 1, 1, 0, 0, 1, 1 };
		private static readonly int[] LocalDz = { 0, 0, 0, 0, 1, 1, 1, 1 };

		public static (DokRowMajor A, Vector b) CreateLinearSystem(int numElementsY)
		{
			if (numElementsY < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(numElementsY));
			}

			int numElementsX = 3 * numElementsY;
			int numElementsZ = 2 * numElementsY;

			// All nodes on x = 0 are fixed, leaving numElementsX free
			// nodes in each (y,z) row.
			int numUnknowns = 3 * numElementsX * (numElementsY + 1) * (numElementsZ + 1);

			double hx = LengthX / numElementsX;
			double hy = LengthY / numElementsY;
			double hz = LengthZ / numElementsZ;

			var A = DokRowMajor.CreateEmpty(numUnknowns, numUnknowns);
			var b = Vector.CreateZero(numUnknowns);

			double lambda = ElasticityModulus * PoissonRatio /
				((1.0 + PoissonRatio) * (1.0 - 2.0 * PoissonRatio));
			double mu = ElasticityModulus / (2.0 * (1.0 + PoissonRatio));

			double[,] D = CreateConstitutiveMatrix(lambda, mu);
			double[,] Ke = CreateElementStiffnessMatrix(hx, hy, hz, D);

			// Assemble the global stiffness matrix.
			for (int k = 0; k < numElementsZ; k++)
			{
				for (int j = 0; j < numElementsY; j++)
				{
					for (int i = 0; i < numElementsX; i++)
					{
						int[] nodes = new int[8];

						for (int a = 0; a < 8; a++)
						{
							nodes[a] = GlobalNode(i + LocalDx[a], j + LocalDy[a], k + LocalDz[a], numElementsX, numElementsY);
						}

						var dofs = new int[24];

						for (int a = 0; a < 8; a++)
						{
							dofs[3 * a] = GetDof(nodes[a], 0, numElementsX, numElementsY);
							dofs[3 * a + 1] = GetDof(nodes[a], 1, numElementsX, numElementsY);
							dofs[3 * a + 2] = GetDof(nodes[a], 2, numElementsX, numElementsY);
						}

						for (int a = 0; a < 24; a++)
						{
							int row = dofs[a];

							if (row < 0)
							{
								continue;
							}

							for (int c = 0; c < 24; c++)
							{
								int col = dofs[c];

								if (col >= 0)
								{
									A.AddToEntry(row, col, Ke[a, c]);
								}
							}
						}
					}
				}
			}

			// Apply a downward nodal load in the y direction to all nodes
			// on the right face x = LengthX.
			// The total vertical load is -TotalLoad.
			int numRightFaceNodes = (numElementsY + 1) * (numElementsZ + 1);
			double loadYPerNode = -TotalLoadY / numRightFaceNodes;
			double loadZPerNode = -TotalLoadZ / numRightFaceNodes;

			for (int k = 0; k <= numElementsZ; k++)
			{
				for (int j = 0; j <= numElementsY; j++)
				{
					int node = GlobalNode(numElementsX, j, k, numElementsX, numElementsY);
					int dofY = GetDof(node, 1, numElementsX, numElementsY);
					int dofZ = GetDof(node, 2, numElementsX, numElementsY);
					b[dofY] += loadYPerNode;
					b[dofZ] += loadZPerNode;
				}
			}

			return (A, b);
		}

		public static IModel CreateModel(int numElementsY)
		{
			int numElementsX = 3 * numElementsY;
			int numElementsZ = 2 * numElementsY;
			int numNodesX = numElementsX + 1;
			int numNodesY = numElementsY + 1;
			int numNodesZ = numElementsZ + 1;
			double hx = LengthX / numElementsX;
			double hy = LengthY / numElementsY;
			double hz = LengthZ / numElementsZ;

			var model = new Model();
			for (int k = 0; k < numNodesZ; k++)
			{
				for (int j = 0; j < numNodesY; j++)
				{
					for (int i = 0; i < numNodesX; i++)
					{
						int nodeID = GlobalNode(i, j, k, numElementsX, numElementsY);
						double x = i * hx;
						double y = j * hy;
						double z = k * hz;
						model.NodesDictionary[nodeID] = new Node(nodeID, x, y, z);
					}
				}
			}

			return model;
		}

		public static ISubdomainFreeDofOrdering OrderDofs(int numElementsY)
		{
			int numElementsX = 3 * numElementsY;
			int numElementsZ = 2 * numElementsY;
			int numNodesX = numElementsX + 1;
			int numNodesY = numElementsY + 1;
			int numNodesZ = numElementsZ + 1;

			var dofTypes = new ActiveDofs();
			dofTypes.AddDof(StructuralDof.TranslationX);
			dofTypes.AddDof(StructuralDof.TranslationY);
			dofTypes.AddDof(StructuralDof.TranslationZ);

			var freeDofs = new IntDofTable();
			int dofID = 0;
			for (int k = 0; k < numNodesZ; k++)
			{
				for (int j = 0; j < numNodesY; j++)
				{
					for (int i = 0; i < numNodesX; i++)
					{
						if (i == 0) continue; // At x = 0, all dofs are constrained

						int nodeID = GlobalNode(i, j, k, numElementsX, numElementsY);

						for (int dofType = 0; dofType < 3; dofType++) // In order: translation x, translation y, translation z
						{
							freeDofs.TryAdd(nodeID, dofType, dofID);
							dofID++;
						}
					}
				}
			}

			return new SubdomainFreeDofOrderingGeneral(dofID, freeDofs, dofTypes);
		}

		private static double[,] CreateConstitutiveMatrix(double lambda, double mu)
		{
			var D = new double[6, 6];

			// Strain/stress ordering: [xx, yy, zz, xy, yz, xz].
			// Engineering shear strains are used.
			D[0, 0] = lambda + 2.0 * mu;
			D[1, 1] = lambda + 2.0 * mu;
			D[2, 2] = lambda + 2.0 * mu;

			D[0, 1] = lambda;
			D[0, 2] = lambda;
			D[1, 0] = lambda;
			D[1, 2] = lambda;
			D[2, 0] = lambda;
			D[2, 1] = lambda;

			D[3, 3] = mu;
			D[4, 4] = mu;
			D[5, 5] = mu;

			return D;
		}

		private static double[,] CreateElementStiffnessMatrix(double hx, double hy, double hz, double[,] D)
		{
			var Ke = new double[24, 24];

			double[] gauss = { -1.0 / Math.Sqrt(3.0), 1.0 / Math.Sqrt(3.0) };
			double detJ = hx * hy * hz / 8.0;

			for (int qz = 0; qz < 2; qz++)
			{
				for (int qy = 0; qy < 2; qy++)
				{
					for (int qx = 0; qx < 2; qx++)
					{
						double xi = gauss[qx];
						double eta = gauss[qy];
						double zeta = gauss[qz];

						var dNdxi = new double[8];
						var dNdeta = new double[8];
						var dNdzeta = new double[8];

						for (int a = 0; a < 8; a++)
						{
							double sx = LocalDx[a] == 0 ? -1.0 : 1.0;
							double sy = LocalDy[a] == 0 ? -1.0 : 1.0;
							double sz = LocalDz[a] == 0 ? -1.0 : 1.0;

							dNdxi[a] = sx * (1.0 + sy * eta) * (1.0 + sz * zeta) / 8.0;
							dNdeta[a] = sy * (1.0 + sx * xi) * (1.0 + sz * zeta) / 8.0;
							dNdzeta[a] = sz * (1.0 + sx * xi) * (1.0 + sy * eta) / 8.0;
						}

						var B = new double[6, 24];

						for (int a = 0; a < 8; a++)
						{
							double dNdx = dNdxi[a] * 2.0 / hx;
							double dNdy = dNdeta[a] * 2.0 / hy;
							double dNdz = dNdzeta[a] * 2.0 / hz;

							int ux = 3 * a;
							int uy = ux + 1;
							int uz = ux + 2;

							B[0, ux] = dNdx;
							B[1, uy] = dNdy;
							B[2, uz] = dNdz;

							B[3, ux] = dNdy;
							B[3, uy] = dNdx;

							B[4, uy] = dNdz;
							B[4, uz] = dNdy;

							B[5, ux] = dNdz;
							B[5, uz] = dNdx;
						}

						// Ke += B^T * D * B * detJ.
						for (int a = 0; a < 24; a++)
						{
							for (int c = 0; c < 24; c++)
							{
								double value = 0.0;

								for (int alpha = 0; alpha < 6; alpha++)
								{
									for (int beta = 0; beta < 6; beta++)
									{
										value += B[alpha, a] * D[alpha, beta] * B[beta, c];
									}
								}

								Ke[a, c] += value * detJ;
							}
						}
					}
				}
			}

			return Ke;
		}

		private static int GlobalNode(int i, int j, int k, int numElementsX, int numElementsY)
		{
			return (k * (numElementsY + 1) + j) * (numElementsX + 1) + i;
		}

		private static int GetDof(int node, int component, int numElementsX, int numElementsY)
		{
			int i = node % (numElementsX + 1);
			int j = (node / (numElementsX + 1)) % (numElementsY + 1);
			int k = node / ((numElementsX + 1) * (numElementsY + 1));

			// Fully fix the left face: ux = uy = uz = 0.
			if (i == 0)
			{
				return -1;
			}

			// Free nodes are numbered with x varying fastest,
			// followed by y and then z. Components are interleaved.
			int freeNode = (k * (numElementsY + 1) + j) * numElementsX + (i - 1);
			return 3 * freeNode + component;
		}
	}
}
