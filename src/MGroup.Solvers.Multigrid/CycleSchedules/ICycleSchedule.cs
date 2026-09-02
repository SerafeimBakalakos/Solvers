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
		/// <summary>
		/// Finds the direction for the next level in the schedule or the end of the current cycle. 
		/// </summary>
		/// <returns>
		/// +1 for moving to lvl+1 (coarser) OR -1 for moving to lvl-1 (finer) OR 0 if the current level is the final one of the cycle.
		/// </returns>
		int MoveNext();

		/// <summary>
		/// Reset to the start of the first cycle.
		/// </summary>
		void Reset();
	}
}
