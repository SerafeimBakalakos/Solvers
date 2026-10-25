namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;

	public class Elasticity3DCartesianCandidateSelection : INearNullspaceCandidatesSelectionStrategy
	{
		private readonly IModel model;
		private readonly int dofUx;
		private readonly int dofUy;
		private readonly int dofUz;
		private readonly double xc;
		private readonly double yc;
		private readonly double zc;
		private readonly ISubdomainFreeDofOrdering dofOrdering;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="model">The FEM model.</param>
		/// <param name="referencePoint">
		/// The (x,y) coordinates of a reference points around which the local numspaces will be centered. The origin or domain centroid are common choices. 
		/// </param>
		public Elasticity3DCartesianCandidateSelection(IModel model, ISubdomainFreeDofOrdering dofOrdering, IDofType translationX, IDofType translationY, IDofType translationZ, double[] referencePoint)
		{
			this.model = model;
			this.dofOrdering = dofOrdering;

			if (referencePoint.Length != 3)
			{
				throw new ArgumentException("A reference point with 3 coordinates (x, y, z) must be provided.");
			}
			this.xc = referencePoint[0];
			this.yc = referencePoint[1];
			this.zc = referencePoint[2];

			this.dofUx = dofOrdering.AllDofs.GetIdOfDof(translationX);
			this.dofUy = dofOrdering.AllDofs.GetIdOfDof(translationY);
			this.dofUz = dofOrdering.AllDofs.GetIdOfDof(translationZ);
		}


		public Matrix ChooseCandidateVectors(IReadOnlyMatrix systemMatrix)
		{
			if (systemMatrix.NumColumns != dofOrdering.NumFreeDofs)
			{
				throw new NonMatchingDimensionsException(
					$"The system matrix is ({systemMatrix.NumRows} x {systemMatrix.NumColumns}), but there are {dofOrdering.NumFreeDofs} free dofs.");
			}

			int numRows = systemMatrix.NumColumns;
			var matrixB = Matrix.CreateZero(numRows, 6);
			foreach ((int nodeID, int dofTypeID, int freeDofID) in dofOrdering.FreeDofs)
			{
				INode node = model.GetNode(nodeID);
				if (dofTypeID == dofUx)
				{
					matrixB[freeDofID, 0] = 1.0;
					matrixB[freeDofID, 4] = node.Z - zc;
					matrixB[freeDofID, 5] = yc - node.Y;
					// columns 1,2,3 have zero entries for this row
				}
				else if (dofTypeID == dofUy)
				{
					matrixB[freeDofID, 1] = 1.0; 
					matrixB[freeDofID, 3] = zc - node.Z;
					matrixB[freeDofID, 5] = node.X - xc;
					// columns 0,2,4 have zero entries for this row
				}
				else if (dofTypeID == dofUz)
				{
					matrixB[freeDofID, 2] = 1.0;
					matrixB[freeDofID, 3] = node.Y - yc;
					matrixB[freeDofID, 4] = xc - node.X;
					// columns 0,1,5 have zero entries for this row
				}
				else
				{
					throw new Exception("Found a freedom degree that is not translation x, translation y or translation z.");
				}
			}

			return matrixB;
		}
	}
}
