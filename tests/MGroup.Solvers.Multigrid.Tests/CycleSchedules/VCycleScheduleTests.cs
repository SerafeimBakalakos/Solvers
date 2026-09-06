namespace MGroup.Solvers.Multigrid.Tests.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.CycleSchedules;

	using Xunit;

	public static class VCycleScheduleTests
	{
		[Fact]
		public static void TestCase1()
		{
			int numLevels = 5;
			int[] expectedPath = { 0, 1, 2, 3, 4, 3, 2, 1, 0 };
			var schedule = new VCycleSchedule();
			LevelProgression progression = schedule.CreateProgression(numLevels);
			Utilities.AssertCycleSchedule(expectedPath, progression, 10);
		}
	}
}
