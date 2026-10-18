namespace MGroup.Solvers.Multigrid.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class MultigridOperatorHierarchyLog
	{
		public int NumLevels { get; set; }

		private List<SparseMatrixSizeLog> ProlongationMatrixSizesPerLevel { get; } = new List<SparseMatrixSizeLog>();

		private List<SparseMatrixSizeLog> RestrictionMatrixSizesPerLevel { get; } = new List<SparseMatrixSizeLog>();

		private List<SparseMatrixSizeLog> SystemMatrixSizesPerLevel { get; } = new List<SparseMatrixSizeLog>();

		public SparseMatrixSizeLog GetProlongationSize(int level) => ProlongationMatrixSizesPerLevel[level];

		public SparseMatrixSizeLog GetRestrictionSize(int level) => RestrictionMatrixSizesPerLevel[level];

		public SparseMatrixSizeLog GetSystemMatrixSize(int level) => SystemMatrixSizesPerLevel[level];

		public void LogProlongation(ISparseMatrix matrix) 
			=> ProlongationMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(matrix.NumRows, matrix.NumColumns, matrix.CountNonZeros()));

		public void LogProlongation(int numRows, int numColums, int numNonZeros)
			=> ProlongationMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(numRows, numColums, numNonZeros));

		public void LogRestriction(ISparseMatrix matrix)
			=> RestrictionMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(matrix.NumRows, matrix.NumColumns, matrix.CountNonZeros()));

		public void LogRestriction(int numRows, int numColums, int numNonZeros)
			=> RestrictionMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(numRows, numColums, numNonZeros));

		public void LogSystemMatrix(ISparseMatrix matrix)
			=> SystemMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(matrix.NumRows, matrix.NumColumns, matrix.CountNonZeros()));

		public void LogSystemMatrix(int numRows, int numColums, int numNonZeros)
			=> SystemMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(numRows, numColums, numNonZeros));
	}
}
