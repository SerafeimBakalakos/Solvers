namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class Cantilever2DElasticityProblem
	{
		public const double LengthX = 2.0;
		public const double LengthY = 1.0;

		private const double TotalLoad = 6000.0;
		private const double ElasticityModulus = 9.0e5;
		private const double PoissonRatio = 0.3;
		private const double Thickness = 1;

		private static readonly int[] LocalDx = { 0, 1, 1, 0 };
		private static readonly int[] LocalDy = { 0, 0, 1, 1 };

		public static (DokRowMajor A, Vector b) CreateLinearSystem(int numElementsY)
		{
			if (numElementsY < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(numElementsY));
			}

			int numElementsX = 2 * numElementsY;
			int numUnknowns = 2 * numElementsX * (numElementsY + 1);

			double hx = LengthX / numElementsX;
			double hy = LengthY / numElementsY;

			var A = DokRowMajor.CreateEmpty(numUnknowns, numUnknowns);
			var b = Vector.CreateZero(numUnknowns);

			double[,] D = CreateConstitutiveMatrix(ElasticityModulus, PoissonRatio);
			double[,] gaussPoints =
			{
				{ -1.0 / Math.Sqrt(3.0), -1.0 / Math.Sqrt(3.0) },
				{  1.0 / Math.Sqrt(3.0), -1.0 / Math.Sqrt(3.0) },
				{  1.0 / Math.Sqrt(3.0),  1.0 / Math.Sqrt(3.0) },
				{ -1.0 / Math.Sqrt(3.0),  1.0 / Math.Sqrt(3.0) }
			};

			double[,] Ke = CreateElementStiffnessMatrix(hx, hy, D, gaussPoints);

			// Assemble the global stiffness matrix.
			// Local node order:
			// 3 -------- 2
			// |          |
			// |          |
			// 0 -------- 1
			//
			// Local DOFs: [u0, v0, u1, v1, u2, v2, u3, v3].
			for (int j = 0; j < numElementsY; j++)
			{
				for (int i = 0; i < numElementsX; i++)
				{
					int[] nodes =
					{
						GlobalNode(i,     j,     numElementsX),
						GlobalNode(i + 1, j,     numElementsX),
						GlobalNode(i + 1, j + 1, numElementsX),
						GlobalNode(i,     j + 1, numElementsX)
					};

					var dofs = new int[8];

					for (int a = 0; a < 4; a++)
					{
						dofs[2 * a] = GetDof(nodes[a], 0, numElementsX);
						dofs[2 * a + 1] = GetDof(nodes[a], 1, numElementsX);
					}

					for (int a = 0; a < 8; a++)
					{
						int row = dofs[a];
						if (row < 0)
						{
							continue;
						}

						for (int c = 0; c < 8; c++)
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

			// Apply a downward nodal load at every node on the right edge.
			// The sum of all vertical nodal loads is -TotalLoad.
			double loadPerNode = -TotalLoad / (numElementsY + 1);

			for (int j = 0; j <= numElementsY; j++)
			{
				int node = GlobalNode(numElementsX, j, numElementsX);
				int verticalDof = GetDof(node, 1, numElementsX);
				b[verticalDof] += loadPerNode;
			}

			return (A, b);
		}

		public static IModel CreateModel(int numElementsY)
		{
			int numElementsX = 2 * numElementsY;
			int numNodesX = numElementsX + 1;
			int numNodesY = numElementsY + 1;
			double hx = LengthX / numElementsX;
			double hy = LengthY / numElementsY;

			var model = new Model();
			model.SubdomainsDictionary[0] = new Subdomain(0);

			// Nodes
			for (int j = 0; j < numNodesY; j++)
			{
				for (int i = 0; i < numNodesX; i++)
				{
					int nodeID = GlobalNode(i, j, numElementsX);
					double x = i * hx;
					double y = j * hy;
					model.NodesDictionary[nodeID] = new Node(nodeID, x, y);
				}
			}

			// Materials
			var material = new ElasticMaterial2D(ElasticityModulus, PoissonRatio, StressState2D.PlaneStress);
			var dynamicProps = new TransientAnalysisProperties(1.0, 0.0, 0.0);

			// Elements
			var elemFactory = new ContinuumElement2DFactory(Thickness, material, dynamicProps);
			int elementID = 0;
			for (int j = 0; j < numElementsY; j++)
			{
				for (int i = 0; i < numElementsX; i++)
				{
					INode[] nodes =
					{
						model.GetNode(GlobalNode(i,     j,     numElementsX)),
						model.GetNode(GlobalNode(i + 1, j,     numElementsX)),
						model.GetNode(GlobalNode(i + 1, j + 1, numElementsX)),
						model.GetNode(GlobalNode(i,     j + 1, numElementsX))
					};

					ContinuumElement2D element = elemFactory.CreateElement(CellType.Quad4, nodes);
					element.ID = elementID;
					model.ElementsDictionary.Add(elementID, element);
					model.SubdomainsDictionary[0].Elements.Add(element);

					elementID++;
				}
			}

			// Clamp boundary condition at one end
			var tol = 1E-10; //TODO: this should be chosen w.r.t. the element size along X
			var dirichletBCs = new List<INodalDisplacementBoundaryCondition>();
			foreach (var node in model.NodesDictionary.Values.Where(node => Math.Abs(node.X) <= tol))
			{
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationX, 0));
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationY, 0));
			}

			// Apply concentrated load at the other end
			var loadedNodes = model.NodesDictionary.Values.Where(node => Math.Abs(node.X - LengthX) <= tol).ToArray();
			var loadPerNode = - TotalLoad / loadedNodes.Length;
			var neumannBCs = new List<INodalLoadBoundaryCondition>();
			foreach (var node in loadedNodes)
			{
				neumannBCs.Add(new NodalLoad(node, StructuralDof.TranslationY, loadPerNode));
			}

			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(dirichletBCs, neumannBCs));


			return model;
		}

		public static double[,] CreateNullspaceCandidateVectors(int numElementsY)
		{
			int numElementsX = 2 * numElementsY;
			int numNodesX = numElementsX + 1;
			int numNodesXFree = numNodesX - 1; // The system retains nodes with i = 1, ..., numElementsX.
			int numNodesY = numElementsY + 1;
			double hx = LengthX / numElementsX;
			double hy = LengthY / numElementsY;

			double xc = LengthX / 2.0;
			double yc = LengthY / 2.0;

			int numDofsFree = 2 * numNodesXFree * numNodesY;
			var B = new double[numDofsFree, 3];

			for (int j = 0; j < numNodesY; j++)
			{
				for (int i = 1; i < numNodesX; i++)
				{
					int nodeID = j * numNodesXFree + (i - 1);
					int ux = 2 * nodeID;
					int uy = 2 * nodeID + 1;
					double x = i * hx;
					double y = j * hy;

					B[ux, 0] = 1.0;
					B[uy, 1] = 1.0;
					B[ux, 2] = -(y - yc);
					B[uy, 2] = x - xc;
				}
			}

			return B;
		}

		public static ISubdomainFreeDofOrdering OrderDofs(int numElementsY)
		{
			int numElementsX = 2 * numElementsY;
			int numNodesX = numElementsX + 1;
			int numNodesY = numElementsY + 1;

			var dofTypes = new ActiveDofs();
			dofTypes.AddDof(StructuralDof.TranslationX);
			dofTypes.AddDof(StructuralDof.TranslationY);

			var freeDofs = new IntDofTable();
			int dofID = 0;
			for (int j = 0; j < numNodesY; j++)
			{
				for (int i = 0; i < numNodesX; i++)
				{
					if (i == 0) continue; // At x = 0, all dofs are constrained

					int nodeID = GlobalNode(i, j, numElementsX);

					for (int dofType = 0; dofType < 2; dofType++) // In order: translation x, translation y
					{
						freeDofs.TryAdd(nodeID, dofType, dofID);
						dofID++;
					}
				}
			}

			return new SubdomainFreeDofOrderingGeneral(dofID, freeDofs, dofTypes);
		}

		private static double[,] CreateConstitutiveMatrix(double elasticityModulus, double poissonRatio)
		{
			double factor = elasticityModulus / (1.0 - poissonRatio * poissonRatio);

			return new double[,]
			{
				{ factor, factor * poissonRatio, 0.0 },
				{ factor * poissonRatio, factor, 0.0 },
				{ 0.0, 0.0, factor * (1.0 - poissonRatio) / 2.0 }
			};
		}

		private static double[,] CreateElementStiffnessMatrix(double hx, double hy, double[,] D, double[,] gaussPoints)
		{
			var Ke = new double[8, 8];

			double[] gauss = { -1.0 / Math.Sqrt(3.0), 1.0 / Math.Sqrt(3.0) };
			double detJ = hx * hy / 4.0;

			foreach (double xi in gauss)
			{
				foreach (double eta in gauss)
				{
					// Derivatives of the four bilinear shape functions.
					double[] dNdxi =
					{
						-(1.0 - eta) / 4.0,
						 (1.0 - eta) / 4.0,
						 (1.0 + eta) / 4.0,
						-(1.0 + eta) / 4.0
					};

					double[] dNdeta =
					{
						-(1.0 - xi) / 4.0,
						-(1.0 + xi) / 4.0,
						 (1.0 + xi) / 4.0,
						 (1.0 - xi) / 4.0
					};

					var B = new double[3, 8];

					for (int a = 0; a < 4; a++)
					{
						double dNdx = dNdxi[a] * 2.0 / hx;
						double dNdy = dNdeta[a] * 2.0 / hy;

						B[0, 2 * a] = dNdx;
						B[1, 2 * a + 1] = dNdy;
						B[2, 2 * a] = dNdy;
						B[2, 2 * a + 1] = dNdx;
					}

					// Ke += B^T * D * B * detJ.
					for (int a = 0; a < 8; a++)
					{
						for (int c = 0; c < 8; c++)
						{
							double value = 0.0;

							for (int alpha = 0; alpha < 3; alpha++)
							{
								for (int beta = 0; beta < 3; beta++)
								{
									value += B[alpha, a] * D[alpha, beta] * B[beta, c];
								}
							}

							Ke[a, c] += value * detJ;
						}
					}
				}
			}

			return Ke;
		}

		private static int GlobalNode(int i, int j, int numElementsX)
		{
			return j * (numElementsX + 1) + i;
		}

		private static int GetDof(int node, int component, int numElementsX)
		{
			int i = node % (numElementsX + 1);
			int j = node / (numElementsX + 1);

			// Fully support the left edge: ux = uy = 0.
			if (i == 0)
			{
				return -1;
			}

			// Each row contains numElementsX free nodes.
			// Components are interleaved: ux, uy.
			return 2 * (j * numElementsX + (i - 1)) + component;
		}
	}
}
