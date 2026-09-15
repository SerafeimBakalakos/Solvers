namespace MGroup.Solvers.Multigrid.GridDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.MSolve.Discretization.Meshes.Structured;

	public interface IGrid
	{
		int[] AxesMajorToMinor { get; }

		/// <summary>
		/// 1D, 2D, or 3D
		/// </summary>
		int Dimension { get; }

		int[] NumNodesPerAxis {get;}

		IGrid CreateGridWithSameSettings(int[] numNodes);

		IStructuredMesh CreateMesh(double[] minCoords, double[] maxCoords);
	}
}
