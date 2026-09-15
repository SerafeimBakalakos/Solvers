namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.Utilities;

	public class Prolongation2DScalarStrategy : IProlongationStrategy
	{
		private Prolongation1DStrategy prolongation1D;

		public Prolongation2DScalarStrategy(double tolerance = 1E-12)
		{
			prolongation1D = new Prolongation1DStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(IGrid fineGrid, IGrid coarseGrid)
		{
			ProlongationUtilities.CheckGrids2D(fineGrid, coarseGrid);

			// Find the 1D prolongation matrix per axis
			int nfx = fineGrid.NumNodesPerAxis[0];
			int nfy = fineGrid.NumNodesPerAxis[1];
			int ncx = coarseGrid.NumNodesPerAxis[0];
			int ncy = coarseGrid.NumNodesPerAxis[1];

			DokRowMajor? Px = (nfx > ncx) ? prolongation1D.CreateProlongationMatrix(nfx, ncx) : null;
			DokRowMajor? Py = (nfy > ncy) ? prolongation1D.CreateProlongationMatrix(nfy, ncy) : null;

			// Combine them to 2D using tensor products
			int majorAxis = fineGrid.AxesMajorToMinor[0];
			if (!ArrayUtilities.Equal(fineGrid.AxesMajorToMinor, coarseGrid.AxesMajorToMinor))
			{
				throw new NotImplementedException();
			}

			DokRowMajor? prolongation2D;
			if (majorAxis == 0) // x major, y minor
			{
				prolongation2D = ProlongationUtilities.KroneckerProduct(Px, nfx, Py, nfy);
			}
			else // y major, x minor
			{
				prolongation2D = ProlongationUtilities.KroneckerProduct(Py, nfy, Px, nfy);
			}

			if (prolongation2D is null) throw new Exception("This should not have happened. Both 1D prolongation matrices cannot be identity.");
			return prolongation2D;
		}
				
	}
}
