namespace MGroup.Solvers.DDM.SolversExtensions.ProblemDefinition
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers;

	public interface ISuperElement
	{
		IntDofTable GetDofs();

		IMatrix BuildMatrix();

		IVector BuildRhsVector();
	}
}
