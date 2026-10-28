namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.NearNullspaceCandidates;

	public class MockElasticityNullspaceCandidateSelection : INearNullspaceCandidatesSelectionStrategy
	{
		private readonly Matrix matrixB;

		public MockElasticityNullspaceCandidateSelection(double[,] matrixB)
		{
			this.matrixB = Matrix.CreateFromArray(matrixB);
		}

		public Matrix ChooseCandidateVectors(IReadOnlyMatrix systemMatrix)
		{
			return matrixB;
		}
	}
}
