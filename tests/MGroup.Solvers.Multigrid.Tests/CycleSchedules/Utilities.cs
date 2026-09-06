namespace MGroup.Solvers.Multigrid.Tests.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Solvers.Multigrid.CycleSchedules;

	using Xunit;

	internal class Utilities
	{
		internal static void AssertCycleSchedule(int[] expectedPath, LevelProgression progression, int numCycles = 2)
		{
			int numSteps = expectedPath.Length;
			for (int c = 0; c < numCycles; c++)
			{
				int expectedMove;
				int computedMove;

				// Check steps other than the last one
				for (int s = 0; s < numSteps - 1; s++)
				{
					expectedMove = expectedPath[s + 1] - expectedPath[s];
					computedMove = progression.MoveNext();
					Assert.Equal(expectedMove, computedMove);
				}

				// Last step
				expectedMove = 0;
				computedMove = progression.MoveNext();
				Assert.Equal(expectedMove, computedMove);
			}
		}
	}
}
