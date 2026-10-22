namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.Solvers.DofOrdering;

	public class Elasticity2DCartesianCandidateSelection : INearNullspaceCandidatesSelectionStrategy
	{
		private readonly IModel model;
		private readonly int dofUx;
		private readonly int dofUy;
		private readonly double xc;
		private readonly double yc;
		private readonly ISubdomainFreeDofOrdering dofOrdering;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="model">The FEM model.</param>
		/// <param name="referencePoint">
		/// The (x,y) coordinates of a reference points around which the local numspaces will be centered. The origin or domain centroid are common choices. 
		/// </param>
		public Elasticity2DCartesianCandidateSelection(IModel model, ISubdomainFreeDofOrdering dofOrdering, IDofType translationX, IDofType translationY, double[] referencePoint)
		{
			this.model = model;
			this.dofOrdering = dofOrdering;

			if (referencePoint.Length != 2)
			{
				throw new ArgumentException("A reference point with 2 coordinates (x, y) must be provided.");
			}
			this.xc = referencePoint[0];
			this.yc = referencePoint[1];

			this.dofUx = dofOrdering.AllDofs.GetIdOfDof(translationX);
			this.dofUy = dofOrdering.AllDofs.GetIdOfDof(translationY);
		}


		public Matrix ChooseCandidateVectors(IReadOnlyMatrix systemMatrix)
		{
			if (systemMatrix.NumColumns != dofOrdering.NumFreeDofs)
			{
				throw new NonMatchingDimensionsException(
					$"The system matrix is ({systemMatrix.NumRows} x {systemMatrix.NumColumns}), but there are {dofOrdering.NumFreeDofs} free dofs.");
			}

			int numRows = systemMatrix.NumColumns;
			var matrixB = Matrix.CreateZero(numRows, 3);
			foreach ((int nodeID, int dofTypeID, int freeDofID) in dofOrdering.FreeDofs)
			{
				INode node = model.GetNode(nodeID);
				//IDofType dofType = dofTypes.GetDofWithId(dofTypeID);
				//if (dofType is StructuralDof.TranslationX)
				if (dofTypeID == dofUx)
				{
					matrixB[freeDofID, 0] = 1.0;
					//matrixB[freeDofID, 1] = 0.0; // Not needed
					matrixB[freeDofID, 2] = yc - node.Y;
				}
				//else if (dofType is StructuralDof.TranslationY)
				else if (dofTypeID == dofUy)
				{
					//matrixB[freeDofID, 0] = 0.0; // Not needed
					matrixB[freeDofID, 1] = 1.0; 
					matrixB[freeDofID, 2] = node.X - xc;
				}
				else
				{
					throw new Exception("Found a freedom degree that is neither translation x nor translation y.");
				}
			}

			return matrixB;
		}
	}
}
