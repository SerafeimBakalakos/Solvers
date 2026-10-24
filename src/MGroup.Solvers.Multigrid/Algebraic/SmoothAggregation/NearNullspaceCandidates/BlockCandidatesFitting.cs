namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.LinearAlgebraExtensions.Orthogonalization;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;

	public class BlockCandidatesFitting : INullspaceCandidatesFittingStrategy
	{
		private readonly double orthonormalizationTolerance;
		private readonly ModifiedGramSchmidt mgs;

		public BlockCandidatesFitting(double orthonormalizationTolerance = 1E-10)
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
			ValidateInput(aggregates, candidatesFine);

			int numGraphNodes = aggregates.NumGraphNodes;
			int numAggregates = aggregates.NumAggregates;
			int numCandidates = candidatesFine.NumColumns;

			int blockSizeFine = candidatesFine.NumRows / numGraphNodes;
			int blockSizeCoarse = numCandidates;
			int[][] graphNodesPerAggregate = aggregates.GatherGraphNodesPerAggregate();

			var prolongationBuilder = new CsrMatrixBuilder(numGraphNodes * blockSizeFine, numAggregates * numCandidates);
			var candidatesCoarse = Matrix.CreateZero(numAggregates * numCandidates, numCandidates);

			// Fit and orthonormalize the candidates independently on each aggregate.
			for (int aggregate = 0; aggregate < numAggregates; ++aggregate)
			{
				int[] graphNodes = graphNodesPerAggregate[aggregate];
				int[] scalarDofsFine = MapGraphNodesToScalarDofs(graphNodes, blockSizeFine);

				// Extract the fine-level candidate rows belonging to this aggregate.
				Matrix localCandidates = candidatesFine.GetSubmatrixRows(scalarDofsFine);

				// Orthonormalize the local candidate columns and obtain their coarse coefficients.
				(Matrix localBasis, Matrix localCoefficients) = mgs.Calculate(localCandidates, orthonormalizationTolerance);

				// Copy the orthonormalized local candidates to the tentative prolongation 
				for (int n = 0; n < graphNodes.Length; ++n) // CHANGE: I must now a copy a block if (blockSizeFine x numCandidates per graphNode.
				{
					int rowStart = blockSizeFine * graphNodes[n];
					int colStart = aggregate * blockSizeCoarse;
					int localRowStart = n * blockSizeFine;

					prolongationBuilder.CopyBlockFrom(rowStart, colStart, localBasis, localRowStart, blockSizeFine, blockSizeCoarse);
				}

				// Copy the orthonormalization coefficents to the coarse-level candidates
				candidatesCoarse.SetSubmatrix(aggregate * numCandidates, 0, localCoefficients);
			}

			CsrMatrix tentativeProlongation = prolongationBuilder.ToCsrMatrix();
			// Optional: drop zero entries from tentativeProlongation

			return (tentativeProlongation, candidatesCoarse, blockSizeCoarse);
		}

		private int[] MapGraphNodesToScalarDofs(int[] graphNodes, int blockSize) //TODO: These can be cached and reused.
		{
			int numGraphNodes = graphNodes.Length;
			var result = new int[blockSize * numGraphNodes];
			for (int n = 0; n < numGraphNodes; ++n)
			{
				int gn = graphNodes[n];
				for (int ib = 0; ib < blockSize; ++ib)
				{
					result[n * blockSize + ib] = gn * blockSize + ib;
				}
			}

			return result;
		}

		private void ValidateInput(AggregateCollection aggregates, Matrix candidatesFine)
		{
			if (candidatesFine == null)
			{
				throw new ArgumentNullException(nameof(candidatesFine));
			}

			if (candidatesFine.NumColumns <= 0)
			{
				throw new ArgumentException("There must be at least one nullspace candidate vector.", nameof(candidatesFine));
			}

			if (candidatesFine.NumRows == 0)
			{
				throw new ArgumentException("The nullspace candidate vectors cannot be 0-dimensional");
			}

			if (aggregates == null)
			{
				throw new ArgumentNullException(nameof(aggregates));
			}

			if (aggregates.NumGraphNodes <= 0)
			{
				throw new ArgumentException("There must be at least one graph node.");
			}

			if (candidatesFine.NumRows % aggregates.NumGraphNodes != 0)
			{
				throw new ArgumentException("The length of the nullspace candidate vectors must an integer multiple of the number of graph nodes.", nameof(candidatesFine));
			}
		}

		private class CsrMatrixBuilder
		{
			private DokRowMajor dok;

			public CsrMatrixBuilder(int numRows, int numColumns)
			{
				dok = DokRowMajor.CreateEmpty(numRows, numColumns);
			}

			public void CopyBlockFrom(int thisStartRow, int thisStartCol, Matrix other, int otherStartRow, int numBlockRows, int numBlockCols)
			{
				for (int i = 0; i < numBlockRows; ++i)
				{
					for (int j = 0; j < numBlockCols; ++j)
					{
						dok[thisStartRow + i, thisStartCol + j] = other[otherStartRow + i, j];
					}
				}
			}

			public CsrMatrix ToCsrMatrix() => dok.BuildCsrMatrix(sortColsOfEachRow: true);
		}
	}
}
