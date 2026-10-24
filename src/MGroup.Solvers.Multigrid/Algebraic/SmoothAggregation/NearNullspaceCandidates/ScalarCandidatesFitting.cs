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

	public class ScalarCandidatesFitting : INullspaceCandidatesFittingStrategy
	{
		private readonly double orthonormalizationTolerance;
		private readonly ModifiedGramSchmidt mgs;

		public ScalarCandidatesFitting(double orthonormalizationTolerance = 1E-10)
		{
			if (orthonormalizationTolerance < 0.0)
			{
				throw new ArgumentOutOfRangeException(nameof(orthonormalizationTolerance));
			}

			this.orthonormalizationTolerance = orthonormalizationTolerance;
			this.mgs = new ModifiedGramSchmidt();
		}

		public (CsrMatrix tentativeProlongation, Matrix coarseCandidates, int blockSizeCoarse) FitCandidates(AggregateCollection aggregates, Matrix candidatesFine)
		{
			int numGraphNodes = aggregates.AggregatesOfGraphNodes.Length;
			int numAggregates = aggregates.NumAggregates;
			int numCandidates = candidatesFine.NumColumns;

			if (candidatesFine.NumRows != numGraphNodes)
			{
				throw new ArgumentException("The number of candidate rows must equal the number of graph nodes.", nameof(candidatesFine));
			}

			if (numCandidates != 1)
			{
				throw new ArgumentException("Exactly one nullspace candidate vector is required.", nameof(candidatesFine));
			}

			int[][] graphNodesPerAggregate = aggregates.GatherGraphNodesPerAggregate();
			var prolongationValues = new double[numGraphNodes * numCandidates];
			var prolongationColumnIndices = new int[numGraphNodes * numCandidates];
			var prolongationRowOffsets = new int[numGraphNodes + 1];
			var candidatesCoarse = Matrix.CreateZero(numAggregates * numCandidates, numCandidates);

			// Fit and orthonormalize the candidates independently on each aggregate.
			for (int aggregate = 0; aggregate < numAggregates; ++aggregate)
			{
				int[] graphNodes = graphNodesPerAggregate[aggregate];

				// Extract the fine-level candidate rows belonging to this aggregate.
				Matrix localCandidates = candidatesFine.GetSubmatrixRows(graphNodes);

				// Orthonormalize the local candidate columns and obtain their coarse coefficients.
				(Matrix localBasis, Matrix localCoefficients) = mgs.Calculate(localCandidates, orthonormalizationTolerance);

				// Copy the orthonormalized local candidates to the tentative prolongation
				for (int localRow = 0; localRow < graphNodes.Length; ++localRow)
				{
					int graphNode = graphNodes[localRow];

					for (int candidate = 0; candidate < numCandidates; ++candidate) //PERFORMANCE: numCandidates = 1. There is no need for a loop. 
					{
						int index = graphNode * numCandidates + candidate;
						prolongationValues[index] = localBasis[localRow, candidate];
						prolongationColumnIndices[index] = aggregate * numCandidates + candidate;
					}
				}

				// Copy the orthonormalization coefficents to the coarse-level candidates
				candidatesCoarse.SetSubmatrix(aggregate * numCandidates, 0, localCoefficients); //PERFORMANCE: numCandidates = 1. This is a scalar copy
			}

			// Construct the scalar CSR tentative prolongation.
			for (int graphNode = 0; graphNode < numGraphNodes; ++graphNode)
			{
				prolongationRowOffsets[graphNode + 1] = (graphNode + 1) * numCandidates;
			}

			var tentativeProlongation = CsrMatrix.CreateFromArrays(numGraphNodes, numAggregates * numCandidates, prolongationValues, prolongationColumnIndices, prolongationRowOffsets, checkInput: false);

			return (tentativeProlongation, candidatesCoarse, numCandidates);
		}
	}
}
