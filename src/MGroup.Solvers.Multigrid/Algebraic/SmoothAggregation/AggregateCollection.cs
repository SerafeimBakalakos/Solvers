namespace MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public sealed class AggregateCollection
	{
		public AggregateCollection(int[] aggregatesOfFineDofs, int[] fineDofRootsOfAggregates)
		{
			AggregatesOfFineDofs = aggregatesOfFineDofs;
			FineDofRootsOfAggregates = fineDofRootsOfAggregates;
			NumAggregates = fineDofRootsOfAggregates.Length;

			Validate();
		}

		public int[] AggregatesOfFineDofs { get; }

		public int[] FineDofRootsOfAggregates { get; }

		public int NumAggregates { get; }

		public CsrMatrix BuildAggregateMembershipMatrix()
		{
			int numFineDofs = AggregatesOfFineDofs.Length;

			var values = new double[numFineDofs];
			var columnIndices = new int[numFineDofs];
			var rowOffsets = new int[numFineDofs + 1];

			for (int i = 0; i < numFineDofs; ++i)
			{
				values[i] = 1.0;
				columnIndices[i] = AggregatesOfFineDofs[i];
				rowOffsets[i + 1] = i + 1;
			}

			return CsrMatrix.CreateFromArrays(numFineDofs, NumAggregates, values, columnIndices, rowOffsets, false);
		}

		[Conditional("DEBUG")]
		private void Validate()
		{
			for (int i = 0; i < AggregatesOfFineDofs.Length; ++i)
			{
				int aggregate = AggregatesOfFineDofs[i];
				if (aggregate < 0 || aggregate >= NumAggregates)
				{
					throw new ArgumentException("Fine DOF has an invalid aggregate index.");
				}
			}
		}
	}
}
