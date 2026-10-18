namespace MGroup.Solvers.Multigrid.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class SparseMatrixSizeLog
	{
		public SparseMatrixSizeLog(int numRows, int numColumns, int numNonZeros)
		{
			NumRows = numRows;
			NumColumns = numColumns;
			NumNonZeros = numNonZeros;
		}

		public int NumColumns { get; }

		public int NumNonZeros { get; }

		public int NumRows { get; }
	}
}
