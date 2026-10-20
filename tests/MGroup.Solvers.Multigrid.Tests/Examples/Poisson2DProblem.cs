namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class Poisson2DProblem
	{
		public static (DokRowMajor A, Vector b) CreateLinearSystem(int numElementsX, int numElementsY, double constantSourceFunction)
		{
			if (numElementsX < 2) throw new ArgumentOutOfRangeException(nameof(numElementsX));

			if (numElementsY < 2) throw new ArgumentOutOfRangeException(nameof(numElementsY));

			int numUnknownsX = numElementsX - 1;
			int numUnknownsY = numElementsY - 1;
			int numUnknowns = numUnknownsX * numUnknownsY;

			double hx = 1.0 / numElementsX;
			double hy = 1.0 / numElementsY;

			var A = DokRowMajor.CreateEmpty(numUnknowns, numUnknowns);
			var b = Vector.CreateZero(numUnknowns);

			// Local node order:
			// 3 -------- 2
			// |          |
			// |          |
			// 0 -------- 1
			//
			// Bilinear Q1 element stiffness matrix on a rectangle.
			double stiffnessX = hy / (6.0 * hx);
			double stiffnessY = hx / (6.0 * hy);

			double[,] Ke =
			{
				{
					2.0 * stiffnessX + 2.0 * stiffnessY,
					-2.0 * stiffnessX + stiffnessY,
					-stiffnessX - stiffnessY,
					stiffnessX - 2.0 * stiffnessY
				},
				{
					-2.0 * stiffnessX + stiffnessY,
					2.0 * stiffnessX + 2.0 * stiffnessY,
					stiffnessX - 2.0 * stiffnessY,
					-stiffnessX - stiffnessY
				},
				{
					-stiffnessX - stiffnessY,
					stiffnessX - 2.0 * stiffnessY,
					2.0 * stiffnessX + 2.0 * stiffnessY,
					-2.0 * stiffnessX + stiffnessY
				},
				{
					stiffnessX - 2.0 * stiffnessY,
					-stiffnessX - stiffnessY,
					-2.0 * stiffnessX + stiffnessY,
					2.0 * stiffnessX + 2.0 * stiffnessY
				}
			};

			// Midpoint quadrature for each local load entry.
			double loadContribution = constantSourceFunction * hx * hy / 4.0;

			// Global nodes are numbered row by row:
			// node(i,j) = j * (numElementsX + 1) + i.
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

					// Assemble each symmetric off-diagonal pair.
					// For a row-sum-zero element matrix, each pair
					// contributes value * [1 -1; -1 1],
					// where value = -Ke[a,c].
					for (int a = 0; a < 4; a++)
					{
						for (int c = a + 1; c < 4; c++)
						{
							AddStiffnessContribution(
								A,
								nodes[a],
								nodes[c],
								-Ke[a, c],
								numElementsX,
								numElementsY);
						}
					}

					AddLoadContribution(
						b,
						nodes[0],
						nodes[1],
						loadContribution,
						numElementsX,
						numElementsY);

					AddLoadContribution(
						b,
						nodes[2],
						nodes[3],
						loadContribution,
						numElementsX,
						numElementsY);
				}
			}

			return (A, b);
		}

		private static int GlobalNode(int i, int j, int numElementsX)
		{
			return j * (numElementsX + 1) + i;
		}

		private static int GetInteriorDof(int node, int numElementsX, int numElementsY)
		{
			int numNodesX = numElementsX + 1;

			int i = node % numNodesX;
			int j = node / numNodesX;

			if (i == 0 || i == numElementsX ||
				j == 0 || j == numElementsY)
			{
				return -1;
			}

			int numUnknownsX = numElementsX - 1;

			return (j - 1) * numUnknownsX + (i - 1);
		}

		private static void AddStiffnessContribution(DokRowMajor A, int node0, int node1, double value, int numElementsX, int numElementsY)
		{
			int dof0 = GetInteriorDof(node0, numElementsX, numElementsY);

			int dof1 = GetInteriorDof(node1, numElementsX, numElementsY);

			if (dof0 >= 0) A.AddToEntry(dof0, dof0, value);

			if (dof0 >= 0 && dof1 >= 0) A.AddToEntry(dof0, dof1, -value);

			if (dof1 >= 0 && dof0 >= 0) A.AddToEntry(dof1, dof0, -value);

			if (dof1 >= 0) A.AddToEntry(dof1, dof1, value);
		}

		private static void AddLoadContribution(Vector rhs, int node0, int node1, double value, int numElementsX, int numElementsY)
		{
			int dof0 = GetInteriorDof(node0, numElementsX, numElementsY);

			int dof1 = GetInteriorDof(node1, numElementsX, numElementsY);

			if (dof0 >= 0) rhs[dof0] += value;

			if (dof1 >= 0) rhs[dof1] += value;
		}
	}
}
