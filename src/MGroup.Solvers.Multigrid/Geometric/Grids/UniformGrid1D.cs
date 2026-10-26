namespace MGroup.Solvers.Multigrid.Geometric.Grids
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Meshes.Structured;

	public class UniformGrid1D : IUniformGrid
	{
		public UniformGrid1D(int numNodes)
		{
			NumNodesPerAxis = new int[] { numNodes };
		}

		public UniformGrid1D(int[] numNodesPerAxis)
		{
			if (numNodesPerAxis.Length != 1) throw new ArgumentException();
			NumNodesPerAxis = new int[] { numNodesPerAxis[0] };
		}

		public int[] AxesMajorToMinor { get; } = [0];

		public int Dimension => 1;

		public int[] NumNodesPerAxis { get; }

		public IUniformGrid CreateGridWithSameSettings(int[] numNodes) => new UniformGrid1D(numNodes);

		public IStructuredMesh CreateMesh(double[] minCoords, double[] maxCoords)
		{
			var numElements = NumNodesPerAxis[0] - 1;
			var meshBuilder = new UniformCartesianMesh1D.Builder(minCoords[0], maxCoords[1], numElements)
				.SetFirstNodeID(0)
				.SetFirstElementID(0);
			return meshBuilder.BuildMesh();
		}
	}
}
