namespace MGroup.Solvers.Multigrid.GridTransfer.Geometric
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.GridDefinition;
	using MGroup.Solvers.Multigrid.Utilities;

	public class Prolongation3DScalarStrategy : IProlongationStrategy
	{
		private Prolongation1DStrategy prolongation1D;

		public Prolongation3DScalarStrategy(double tolerance = 1E-12)
		{
			prolongation1D = new Prolongation1DStrategy(tolerance);
		}

		public DokRowMajor CreateProlongationMatrix(IGrid fineGrid, IGrid coarseGrid)
		{
			//TODO: the order of operations matters: Pz*(Py*Px) vs (Pz*Py)*Px. Optimize it.
			ProlongationUtilities.CheckGrids3D(fineGrid, coarseGrid);

			// Find the 1D prolongation matrix per axis
			var prolongations1D = new DokRowMajor?[3];
			var numRowsOfProlongations1D = new int[3];
			for (int d = 0; d < 3; d++)
			{
				int nf = fineGrid.NumNodesPerAxis[d];
				int nc = coarseGrid.NumNodesPerAxis[d];
				prolongations1D[d] = (nf > nc) ? prolongation1D.CreateProlongationMatrix(nf, nc) : null;
				numRowsOfProlongations1D[d] = nf;
			}

			// Combine them to 3D using tensor products
			int[] axesMajorToMinor = fineGrid.AxesMajorToMinor;
			if (!ArrayUtilities.Equal(fineGrid.AxesMajorToMinor, coarseGrid.AxesMajorToMinor))
			{
				throw new NotImplementedException();
			}

			DokRowMajor? majorP = prolongations1D[axesMajorToMinor[0]];
			int nMajor = numRowsOfProlongations1D[axesMajorToMinor[0]];
			DokRowMajor? mediumP = prolongations1D[axesMajorToMinor[1]];
			int nMedium = numRowsOfProlongations1D[axesMajorToMinor[1]];
			DokRowMajor? minorP = prolongations1D[axesMajorToMinor[2]];
			int nMinor = numRowsOfProlongations1D[axesMajorToMinor[2]];

			DokRowMajor? prolongation2D = ProlongationUtilities.KroneckerProduct(majorP, nMajor, mediumP, nMedium);
			DokRowMajor? prolongation3D = ProlongationUtilities.KroneckerProduct(prolongation2D, nMajor * nMedium, minorP, nMinor);
			if (prolongation3D is null) throw new Exception("This should not have happened. All 1D prolongation matrices cannot be identity.");
			
			return prolongation3D;
		}
	}
}
