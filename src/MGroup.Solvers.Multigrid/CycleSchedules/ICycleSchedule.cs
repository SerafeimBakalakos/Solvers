namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Represents the level progression inside a multigrid cycle
	/// </summary>
	public interface ICycleSchedule
	{
		LevelProgression CreateProgression(int numLevels);
	}
}
