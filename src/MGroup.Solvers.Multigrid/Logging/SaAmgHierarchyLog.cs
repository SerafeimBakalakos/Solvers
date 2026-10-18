namespace MGroup.Solvers.Multigrid.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public class SaAmgHierarchyLog : MultigridOperatorHierarchyLog
	{
		private List<int> NumAggregatesPerLevel { get; } = new List<int>();

		private List<int> NumNullspaceCandidateVectorsPerLevel { get; } = new List<int>();

		private List<SparseMatrixSizeLog> StrengthOfConnectionMatrixSizesPerLevel { get; } = new List<SparseMatrixSizeLog>();

		private List<SparseMatrixSizeLog> TentativeProlongationMatrixSizesPerLevel { get; } = new List<SparseMatrixSizeLog>();

		public int GetNumAggregates(int level) => NumAggregatesPerLevel[level];

		public int GetNumNullspaceCandidateVectors(int level) => NumNullspaceCandidateVectorsPerLevel[level];

		public SparseMatrixSizeLog GetStrengthOfConnectionSize(int level) => StrengthOfConnectionMatrixSizesPerLevel[level];

		public SparseMatrixSizeLog GetTentativeProlongationSize(int level) => TentativeProlongationMatrixSizesPerLevel[level];

		public void LogNumAggregates(int numAggregates) => NumAggregatesPerLevel.Add(numAggregates);

		public void LogNumNullspaceCandidateVectors(int numVectors) => NumNullspaceCandidateVectorsPerLevel.Add(numVectors);

		public void LogStrengthOfConnection(ISparseMatrix matrix)
			=> StrengthOfConnectionMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(matrix.NumRows, matrix.NumColumns, matrix.CountNonZeros()));

		public void LogStrengthOfConnection(int numRows, int numColums, int numNonZeros)
			=> StrengthOfConnectionMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(numRows, numColums, numNonZeros));

		public void LogTentativeProlongation(ISparseMatrix matrix)
			=> TentativeProlongationMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(matrix.NumRows, matrix.NumColumns, matrix.CountNonZeros()));

		public void LogTentativeProlongation(int numRows, int numColums, int numNonZeros)
			=> TentativeProlongationMatrixSizesPerLevel.Add(new SparseMatrixSizeLog(numRows, numColums, numNonZeros));
	}
}
