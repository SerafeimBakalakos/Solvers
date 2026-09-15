namespace MGroup.MSolve.Discretization.Meshes.Structured
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;

	public class UniformCartesianMesh1D : ICartesianMesh
	{
		private const int dim = 1;
		private const int numNodesPerElement = 2;

		private readonly double dx;
		private readonly int firstElementID;
		private readonly int firstNodeID;
		private readonly double minX;
		private readonly double maxX;

		private UniformCartesianMesh1D(double minCoordinate, double maxCoordinate, int numElements, int firstNodeID, int firstElementID)
		{
			this.minX = minCoordinate;
			this.maxX = maxCoordinate;
			this.MinCoordinates = new double[] { minCoordinate };
			this.MaxCoordinates = new double[] { maxCoordinate };
			this.NumElements = new int[] { numElements };
			this.NumNodes = new int[] { numElements + 1 };

			dx = (maxCoordinate - minCoordinate) / numElements;
			DistancesBetweenPoints = new double[] { dx };

			NumNodesTotal = NumNodes[0];
			NumElementsTotal = NumElements[0];

			this.firstNodeID = firstNodeID;
			this.firstElementID = firstElementID;
		}

		public CellType CellType => CellType.Quad4;

		public int Dimension => dim;

		public double[] DistancesBetweenPoints { get; }

		public double[] MinCoordinates { get; }

		public double[] MaxCoordinates { get; }

		public int[] NumElements { get; }

		public int NumElementsTotal { get; }

		public int[] NumNodes { get; }

		public int NumNodesPerElement => numNodesPerElement;

		public int NumNodesTotal { get; }

		public IEnumerable<(int nodeID, double[] coordinates)> EnumerateNodes()
		{
			for (int i = 0; i < NumNodes[0]; ++i)
			{
				var idx = new int[] {i};
				yield return (GetNodeID(idx), GetNodeCoordinates(idx));
			}
		}

		public IEnumerable<(int elementID, int[] nodeIDs)> EnumerateElements()
		{
			for (int i = 0; i < NumElements[0]; ++i)
			{
				var idx = new int[] {i};
				yield return (GetElementID(idx), GetElementConnectivity(idx));
			}
		}

		public int GetNodeID(int[] nodeIdx)
		{
			CheckNodeIdx(nodeIdx);
			return firstNodeID + nodeIdx[0];
		}

		public int[] GetNodeIdx(int nodeID)
		{
			CheckNodeID(nodeID);
			return new int[] { nodeID - firstNodeID };
		}

		public double[] GetNodeCoordinates(int[] nodeIdx)
		{
			CheckNodeIdx(nodeIdx);
			return new double[] { minX + nodeIdx[0] * dx };
		}

		public int GetElementID(int[] elementIdx)
		{
			CheckElementIdx(elementIdx);
			return firstElementID + elementIdx[0];
		}

		public int[] GetElementIdx(int elementID)
		{
			CheckElementID(elementID);
			return new int[] { elementID - firstElementID };
		}

		public int[] GetElementConnectivity(int[] elementIdx)
		{
			CheckElementIdx(elementIdx);
			int e = elementIdx[0];
			return new int[] { e, e + 1 };
		}

		public int[] GetElementConnectivity(int elementID) => GetElementConnectivity(GetElementIdx(elementID));

		[Conditional("DEBUG")]
		private void CheckElementIdx(int[] elementIdx)
		{
			if (elementIdx.Length != dim)
			{
				throw new ArgumentException($"Element index must be an array with Length = {dim}");
			}

			if ((elementIdx[0] < 0) || (elementIdx[0] >= NumElements[1]))
			{
				throw new ArgumentException($"Element index along dimension 0 must belong in [0, {NumElements[0]})");
			}
		}

		[Conditional("DEBUG")]
		private void CheckElementID(int elementID)
		{
			if ((elementID < firstElementID) || (elementID >= firstElementID + NumElementsTotal))
			{
				throw new ArgumentException(
					$"Element ID must belong in [{firstElementID}, {firstElementID + NumElementsTotal})");
			}
		}

		[Conditional("DEBUG")]
		private void CheckNodeIdx(int[] nodeIdx)
		{
			if (nodeIdx.Length != dim)
			{
				throw new ArgumentException($"Node index must be an array with Length = {dim}");
			}

			if ((nodeIdx[0] < 0) || (nodeIdx[0] >= NumNodes[0]))
			{
				throw new ArgumentException($"Node index along dimension 0 must belong in [0, {NumNodes[0]})");
			}
		}

		[Conditional("DEBUG")]
		private void CheckNodeID(int nodeID)
		{
			if ((nodeID < firstNodeID) || (nodeID >= firstNodeID + NumNodesTotal))
			{
				throw new ArgumentException(
					$"Node ID must belong in [{firstNodeID}, {firstNodeID + NumNodesTotal})");
			}
		}

		public class Builder
		{
			private readonly double coordMin;
			private readonly double coordMax;
			private readonly int numElements;
			private int firstElementID;
			private int firstNodeID;

			/// <summary>
			/// Initializes a new Builder for <see cref="UniformCartesianMesh1D"/>.
			/// </summary>
			/// <param name="minCoordinate">The minimum x coordinate of all nodes.</param>
			/// <param name="maxCoordinate">The maximum x coordinate of all nodes.</param>
			/// <param name="numElements">The number of elements along axis x.</param>
			public Builder(double minCoordinate, double maxCoordinate, int numElements)
			{
				this.coordMin = minCoordinate;
				this.coordMax = maxCoordinate;
				this.numElements = numElements;

				// Defaults
				firstNodeID = 0;
				firstElementID = 0;
			}

			public UniformCartesianMesh1D BuildMesh()
			{
				Validate();
				return new UniformCartesianMesh1D(coordMin, coordMax, numElements, firstNodeID, firstElementID);
			}

			public Builder SetFirstElementID(int elementID)
			{
				this.firstElementID = elementID;
				return this;
			}

			public Builder SetFirstNodeID(int nodeID)
			{
				this.firstNodeID = nodeID;
				return this;
			}

			//TODO: This must be called lazily but only once when building several products with the same builder without
			//      mutating the builder.
			private void Validate()
			{
				// Coordinates
				if (coordMin >= coordMax)
				{
					throw new ArgumentException(
						$"Along axis 0, min coordinates must be strictly less than max coordinates.");
				}

				// Elements per axis
				if (numElements < 1)
				{
					throw new ArgumentException($"Along axis 0, there must be at least 1 element.");
				}

				// First node & element IDs: no illegal values yet.
			}
		}
	}
}
