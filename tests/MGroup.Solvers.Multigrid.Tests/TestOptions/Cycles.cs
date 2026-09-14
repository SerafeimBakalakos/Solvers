namespace MGroup.Solvers.Multigrid.Tests.TestOptions
{
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR;
	using MGroup.Solvers.Multigrid.CycleSchedules;

	public enum Cycles
	{
		V, W, F
	}

	public static class CyclesExtensions
	{
		public static ICycleSchedule Translate(this Cycles option)
		{
			if (option == Cycles.V)
			{
				return new VCycleSchedule();
			}
			else if (option == Cycles.W)
			{
				return new WCycleSchedule();
			}
			else if (option == Cycles.F)
			{
				return new FCycleSchedule();
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
