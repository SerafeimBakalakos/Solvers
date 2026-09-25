namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;

	public static class Poisson1DProblem
	{
		public static (DokRowMajor A, Vector b) CreateWithConstantSource(int numElements, double source = 1.0)
			=> Create(numElements, x => source);

		/// <summary>
		/// Creates the stiffness matrix and load vector for 
		///     -u''(x) = f(x),  x in (0, 1)
		/// with homogeneous Dirichlet boundary conditionsu(0) = u(1) = 0.
		/// The domain is divided into numElements uniform linear finite elements. The boundary DOFs are eliminated, so the returned system has numElements - 1 unknowns.
		/// The stiffness matrix is of the form: K = 1/h * [2 -1 0 0 ... 0; -1 2 -1 0 0 ...; 0 -1 2 -1 0 0 ...; ...] where h = 1 / numElements
		/// </summary>
		public static (DokRowMajor A, Vector b) Create(int numElements, Func<double, double> sourceFunction)
		{
			if (numElements < 2) throw new ArgumentOutOfRangeException(nameof(numElements));

			if (sourceFunction == null) throw new ArgumentNullException(nameof(sourceFunction));

			int numUnknowns = numElements - 1;
			double h = 1.0 / numElements;

			var A = DokRowMajor.CreateEmpty(numUnknowns, numUnknowns);
			var b = Vector.CreateZero(numUnknowns);

			// Local stiffness matrix:
			//     1/h * [ 1  -1 ]
			//          [ -1  1 ]
			double localStiffness = 1.0 / h;

			// Assemble stiffness matrix and load vector.
			// Global nodes are numbered 0 ... numElements.
			// Nodes 0 and numElements are Dirichlet nodes and therefore are not included in the returned system.
			for (int element = 0; element < numElements; element++)
			{
				int node0 = element;
				int node1 = element + 1;
				double x0 = node0 * h;
				double x1 = node1 * h;

				AddStiffnessContribution(A, node0, node1, localStiffness, numElements);

				// Load contribution.
				// For the basic test problem, use one-point quadrature at the element midpoint:
				//     integral(phi_i f) dx
				// approximately
				//     f(x_mid) * h/2
				// for each of the two linear basis functions.
				double xMid = 0.5 * (x0 + x1);
				double fMid = sourceFunction(xMid);
				double loadContribution = fMid * h / 2.0;

				AddLoadContribution(b, node0, node1, loadContribution, numElements);
			}

			return (A, b);
		}

		private static void AddStiffnessContribution(DokRowMajor A, int node0, int node1, double value, int numElements)
		{
			// Convert global node numbers to system DOF numbers.
			// Boundary nodes 0 and numElements are excluded.
			int dof0 = node0 - 1;
			int dof1 = node1 - 1;

			if (node0 != 0) A.AddToEntry(dof0, dof0, value);

			if (node0 != 0 && node1 != numElements) A.AddToEntry(dof0, dof1, -value);

			if (node1 != numElements && node0 != 0) A.AddToEntry(dof1, dof0, -value);

			if (node1 != numElements) A.AddToEntry(dof1, dof1, value);
		}

		private static void AddLoadContribution(Vector rhs, int node0, int node1, double value, int numElements)
		{
			if (node0 != 0)
			{
				int dof0 = node0 - 1;
				rhs[dof0] += value;
			}

			if (node1 != numElements)
			{
				int dof1 = node1 - 1;
				rhs[dof1] += value;
			}
		}
	}
}
