namespace MGroup.Solvers.Multigrid.GridTransfer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions;

	public class Prolongation1DStrategy : IProlongationStrategy
	{
		private double tolerance;

		public Prolongation1DStrategy(double tolerance = 1E-12)
		{
			this.tolerance = tolerance;
		}

		public DokRowMajor CreateProlongationMatrix(int[] numNodesFinePerAxis, int[] numNodesCoarsePerAxis)
		{
			var nf = numNodesFinePerAxis[0];
			var nc = numNodesCoarsePerAxis[0];
			GridPreconditions.CheckGrids1D(nf, nc);

			// Initialize
			var dxf = 1.0 / (nf - 1); // length of fine element, scaled so that domain length = 1
			var dxc = 1.0 / (nc - 1); // length of caorse element, scaled so that domain length = 1
			var prolongation = DokRowMajor.CreateEmpty(nf, nc);

			// Go over each node
			for (var i = 0; i < nf; i++)
			{
				// i = index of fine node. I = index of coarse node.
				var x = i * dxf; // global coordinate (scaled) of fine node

				// The coarse element that contains x has coarse nodes I1, I2. These are the coarse-grid neighbors of the fine node. If the fine node coincides with a coarse node, then the coarse element will be chosen so that the fine node coincides with the left neighbor
				var I1 = (int)Math.Floor(x / dxc);
				if (I1 >= nc)
				{
					// If the fine node lies on the right boundary, then choose the coarse element so that the fine node coincides with the right neighbor. Otherwise we will go outside the domain.
					I1 = nc - 2;
				}

				var I2 = I1 + 1;

				// Find the local coordinate xi (ξ) inside the coarse element.
				var x1 = I1 * dxc; // global coordinate (scaled) of left neighbor
				var xi = (x - x1) / dxc; // xi = 0 if x coincides with I1. xi = 1 if x coincides with I2.

				// Loop over the neighbors
				for (var I = I1; I <= I2; I++)
				{
					double w;
					if (I == I1)
					{
						w = 1 - xi; // Weight for I1 neighbor = shape function
					}
					else
					{
						w = xi; // Weight for I2 neighbor = shape function
					}

					// Store the weight in the prolongation matrix
					if (w > tolerance)
					{
						prolongation[i, I] = w;
					}
					else if (w < -tolerance)
					{
						throw new Exception($"Negative weight at P[{i},{I}]");
					}
					// else do nothing: Do not store zeros (fine node coincides with coarse node)
				}
			}

			return prolongation;
		}
	}
}
