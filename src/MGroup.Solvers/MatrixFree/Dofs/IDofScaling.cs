namespace MGroup.Solvers.MatrixFree.Dofs
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;

	public interface IDofScaling
	{
		void Update();

		DiagonalMatrix GetScalingMatrix(int elementID);
	}
}
