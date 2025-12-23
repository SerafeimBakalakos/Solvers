namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IDofScaling
	{
		void Calculate(PartitionedMatrixGlobal partitionedMatrix);

		DiagonalMatrix GetScalingMatrix(int elementID);
	}
}
