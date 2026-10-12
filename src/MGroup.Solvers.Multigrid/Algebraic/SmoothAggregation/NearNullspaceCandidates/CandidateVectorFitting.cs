namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Orthogonalization;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;

	public class CandidateVectorFitting
	{
		private readonly double orthonormalizationTolerance;
		private readonly ModifiedGramSchmidt mgs;

		public CandidateVectorFitting(double orthonormalizationTolerance = 1E-10)
		{
			if (orthonormalizationTolerance < 0.0)
			{
				throw new ArgumentOutOfRangeException(nameof(orthonormalizationTolerance));
			}

			this.orthonormalizationTolerance = orthonormalizationTolerance;
			this.mgs = new ModifiedGramSchmidt();
		}

		public (CsrMatrix tentativeProlongation, Matrix coarseCandidates) FitCandidates(AggregateCollection aggregates, Matrix fineCandidates)
		{
			int numFineDofs = aggregates.AggregatesOfFineDofs.Length;
			int numAggregates = aggregates.NumAggregates;
			int numCandidates = fineCandidates.NumColumns;
			int numCoarseDofs = numAggregates * numCandidates;

			if (fineCandidates.NumRows != numFineDofs)
			{
				throw new ArgumentException("The number of candidate rows must equal the number of fine DOFs.", nameof(fineCandidates));
			}

			if (numCandidates == 0)
			{
				throw new ArgumentException("At least one candidate is required.", nameof(fineCandidates));
			}

			if (orthonormalizationTolerance < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(orthonormalizationTolerance));
			}

			int[][] fineDofsByAggregate = aggregates.GatherFineDofsPerAggregate();
			var prolongationValues = new double[numFineDofs * numCandidates];
			var prolongationColumnIndices = new int[numFineDofs * numCandidates];
			var prolongationRowOffsets = new int[numFineDofs + 1];
			var coarseCandidates = Matrix.CreateZero(numCoarseDofs, numCandidates);

			// Fit and orthonormalize the candidates independently on each aggregate.
			for (int aggregate = 0; aggregate < numAggregates; ++aggregate)
			{
				int[] fineDofs = fineDofsByAggregate[aggregate];

				// Extract the fine-level candidate rows belonging to this aggregate.
				Matrix localCandidates = fineCandidates.GetSubmatrixRows(fineDofs);

				// Orthonormalize the local candidate columns and obtain their coarse coefficients.
				(Matrix localBasis, Matrix localCoefficients) = mgs.Calculate(localCandidates, orthonormalizationTolerance);

				// Copy the orthonormalized local candidates to the tentative prolongation
				for (int localRow = 0; localRow < fineDofs.Length; ++localRow)
				{
					int fineDof = fineDofs[localRow];

					for (int candidate = 0; candidate < numCandidates; ++candidate)
					{
						int index = fineDof * numCandidates + candidate;
						prolongationValues[index] = localBasis[localRow, candidate];
						prolongationColumnIndices[index] = aggregate * numCandidates + candidate;
					}
				}

				// Copy the orthonormalization coefficents to the coarse-level candidates
				coarseCandidates.SetSubmatrix(aggregate * numCandidates, 0, localCoefficients);
			}

			// Construct the scalar CSR tentative prolongator.
			for (int fineDof = 0; fineDof < numFineDofs; ++fineDof)
			{
				prolongationRowOffsets[fineDof + 1] = (fineDof + 1) * numCandidates;
			}

			var tentativeProlongation = CsrMatrix.CreateFromArrays(numFineDofs, numCoarseDofs, prolongationValues, prolongationColumnIndices, prolongationRowOffsets, checkInput: false);

			return (tentativeProlongation, coarseCandidates);
		}
	}
}
