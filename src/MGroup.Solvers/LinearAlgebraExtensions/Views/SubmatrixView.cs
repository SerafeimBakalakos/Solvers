namespace MGroup.Solvers.LinearAlgebraExtensions.Views
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Matrices;

	public class SubmatrixView : DefaultMatrix
	{
		private readonly IMatrix originalMatrix;
		private readonly int[] rowsColsToKeep;

		public SubmatrixView(IMatrix originalMatrix, int[] rowsColsToKeep)
		{
			this.originalMatrix = originalMatrix;
			this.rowsColsToKeep = rowsColsToKeep;
		}

		public override double this[int rowIdx, int colIdx] 
		{ 
			get
			{
				Preconditions.CheckIndexRow(this, rowIdx);
				Preconditions.CheckIndexCol(this, colIdx);
				return originalMatrix[rowsColsToKeep[rowIdx], rowsColsToKeep[colIdx]];
			}
			set
			{
				Preconditions.CheckIndexRow(this, rowIdx);
				Preconditions.CheckIndexCol(this, colIdx);
				originalMatrix.Set(rowsColsToKeep[rowIdx], rowsColsToKeep[colIdx], value);
			}
		}

		public override int NumColumns => rowsColsToKeep.Length;

		public override int NumRows => rowsColsToKeep.Length;

		public override void Clear()
		{
			foreach (int row in rowsColsToKeep)
			{
				foreach (int col in rowsColsToKeep)
				{
					originalMatrix.Set(row, col, 0.0);
				}
			}
		}

		public override IMatrix CreateZeroMatrixWithSameFormat() 
			=> Matrix.CreateZero(rowsColsToKeep.Length, rowsColsToKeep.Length);

		public override bool HasSameFormat(IReadOnlyMatrix otherMatrix)
		{
			if (otherMatrix is SubmatrixView casted)
			{
				return (casted.originalMatrix == this.originalMatrix) && (casted.rowsColsToKeep == this.rowsColsToKeep);
			}

			return false;
		}
	}
}
