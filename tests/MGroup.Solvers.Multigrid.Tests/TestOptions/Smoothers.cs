namespace MGroup.Solvers.Multigrid.Tests.TestOptions
{
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;

	public enum Smoothers
	{
		Jac, GS, SGS, WJ, SOR, SSOR
	}

	public static class SmoothersExtensions
	{
		public static IStationaryIteration Translate(this Smoothers option, double relaxationFactor)
		{
			if (option == Smoothers.Jac)
			{
				return new JacobiIterationCsr();
			}
			else if (option == Smoothers.GS)
			{
				return new GaussSeidelIterationCsr(true);
			}
			else if (option == Smoothers.SGS)
			{
				return new SgsIterationCsr();
			}
			else if (option == Smoothers.WJ)
			{
				return new WeightedJacobiIterationCsr(relaxationFactor);
			}
			else if (option == Smoothers.SOR)
			{
				return new SorIterationCsr(relaxationFactor, true);
			}
			else if (option == Smoothers.SSOR)
			{
				return new SsorIterationCsr(relaxationFactor);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
