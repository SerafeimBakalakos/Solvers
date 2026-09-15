namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.MSolve.Discretization.Meshes.Structured;

	public class Grid2D : IGrid
	{
		public Grid2D(int numNodesX, int numNodesY)
		{
			NumNodesPerAxis = new int[] { numNodesX, numNodesY };
			AxesMajorToMinor = OrderAxesDefault(NumNodesPerAxis);
		}

		public Grid2D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length != 2) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0], numNodesPerAxis[1] };
			AxesMajorToMinor = OrderAxesDefault(NumNodesPerAxis);
		}

		public int[] AxesMajorToMinor { get; private set; }

		public int Dimension => 2;

		public int[] NumNodesPerAxis { get; }

		IGrid IGrid.CreateGridWithSameSettings(int[] numNodes) => CreateGridWithSameSettings(numNodes);

		public Grid2D CreateGridWithSameSettings(int[] numNodes)
		{
			var result = new Grid2D(numNodes);
			result.AxesMajorToMinor = AxesMajorToMinor;
			return result;
		}

		IStructuredMesh IGrid.CreateMesh(double[] minCoords, double[] maxCoords) => CreateMesh(minCoords, maxCoords);

		public UniformCartesianMesh2D CreateMesh(double[] minCoords, double[] maxCoords)
		{
			var numElements = new int[] { NumNodesPerAxis[0] - 1, NumNodesPerAxis[1] - 1 };
			var meshBuilder = new UniformCartesianMesh2D.Builder(minCoords, maxCoords, numElements)
				.SetElementNodeOrderCounterClockwise()
				.SetMajorAxis(AxesMajorToMinor[0])
				.SetFirstNodeID(0)
				.SetFirstElementID(0);
			return meshBuilder.BuildMesh();
		}

		public void SetMajorAxis(int majorAxis)
		{
			int minorAxis = 1 - majorAxis; // If I sum the axes: 0 + 1 = 1
			AxesMajorToMinor[0] = majorAxis;
			AxesMajorToMinor[1] = minorAxis;
		}

		private static int[] OrderAxesDefault(int[] numNodesPerAxis)
		{
			Debug.Assert(numNodesPerAxis.Length == 2);
			if (numNodesPerAxis[0] <= numNodesPerAxis[1])
			{
				return [0, 1];
			}
			else
			{
				return [1, 0];
			}
		}
	}
}
