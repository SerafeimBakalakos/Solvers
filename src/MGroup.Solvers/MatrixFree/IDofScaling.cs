namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IDofScaling
	{
		void Initialize();

		DiagonalMatrix GetScalingMatrix(int elementID);
	}
}
