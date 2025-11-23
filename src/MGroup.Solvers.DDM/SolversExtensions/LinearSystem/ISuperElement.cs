namespace MGroup.Solvers.DDM.SolversExtensions.LinearSystem
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Solvers;

	public interface ISuperElement
	{
		IntDofTable GetDofs();
	}
}
