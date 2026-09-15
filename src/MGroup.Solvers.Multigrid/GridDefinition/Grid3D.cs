namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.MSolve.Discretization.Meshes.Structured;

	public class Grid3D : IGrid
	{
		public Grid3D(int numNodesX, int numNodesY, int numNodesZ)
		{
			NumNodesPerAxis = new int[] { numNodesX, numNodesY, numNodesZ };
			AxesMajorToMinor = OrderAxesDefault(NumNodesPerAxis);
		}

		public Grid3D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length != 3) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0], numNodesPerAxis[1], numNodesPerAxis[2] };
			AxesMajorToMinor = OrderAxesDefault(NumNodesPerAxis);
		}

		public int[] AxesMajorToMinor { get; private set; }

		public int Dimension => 3;

		public int[] NumNodesPerAxis { get; }

		IGrid IGrid.CreateGridWithSameSettings(int[] numNodes) => CreateGridWithSameSettings(numNodes);

		public Grid3D CreateGridWithSameSettings(int[] numNodes)
		{
			var result = new Grid3D(numNodes);
			result.AxesMajorToMinor = AxesMajorToMinor;
			return result;
		}

		IStructuredMesh IGrid.CreateMesh(double[] minCoords, double[] maxCoords) => CreateMesh(minCoords, maxCoords);

		public UniformCartesianMesh3D CreateMesh(double[] minCoords, double[] maxCoords)
		{
			var numElements = new int[] { NumNodesPerAxis[0] - 1, NumNodesPerAxis[1] - 1 };
			var meshBuilder = new UniformCartesianMesh3D.Builder(minCoords, maxCoords, numElements)
				.SetElementNodeOrderDefault()
				.SetMajorMinorAxis(AxesMajorToMinor[0], AxesMajorToMinor[2])
				.SetFirstNodeID(0)
				.SetFirstElementID(0);
			return meshBuilder.BuildMesh();
		}

		public void SetMajorMinorAxis(int majorAxis, int minorAxis)
		{
			int mediumAxis = 3 - majorAxis - minorAxis; // If I sum the axes: 0 + 1 + 2 = 3
			AxesMajorToMinor[0] = majorAxis;
			AxesMajorToMinor[1] = mediumAxis;
			AxesMajorToMinor[2] = minorAxis;
		}

		private static int[] OrderAxesDefault(int[] numNodesPerAxis)
		{
			Debug.Assert(numNodesPerAxis.Length == 3);
			int min = -1;
			int max = -1;
			for (int d = 0; d < 3; d++)
			{
				if (numNodesPerAxis[d] < min) min = numNodesPerAxis[d];
				if (numNodesPerAxis[d] > max) max = numNodesPerAxis[d];
			}

			var result = new int[3];
			for (int d = 0; d < 3; d++)
			{
				int n = numNodesPerAxis[d];
				if (n == min) result[0] = d;
				else if (n == max) result[2] = d;
				else result[1] = n;
			}

			return result;
		}
	}
}
