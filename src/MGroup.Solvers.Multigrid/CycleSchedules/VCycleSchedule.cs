namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Implements the level progression for V-cycles.
	/// </summary>
	public class VCycleSchedule : CycleScheduleBase
	{
		private VCycleSchedule(int numLevels, int[] path)
			: base(numLevels, path)
		{
		}

		/// <summary>
		/// Constructs an instance of this class.
		/// </summary>
		/// <param name="numLevels">The total number of multigrid levels, including the finest and coarsest grids.</param>
		public static VCycleSchedule Create(int numLevels)
		{
			int numSteps = 2 * numLevels - 1; // 1 pass of the coarsest grid, 2 passes for the rest.

			// Top level (finest)
			var path = new int[numSteps];
			int s = 0;
			path[s] = 0; 

			for (int lvl = 1; lvl < numLevels; lvl++) // Downward sweep: (finest, coarsest]
			{
				s++;
				path[s] = lvl;
			}

			for (int lvl = numLevels - 2; lvl >= 0; lvl--) // Upward sweep: (coarsest, finest]
			{
				s++;
				path[s] = lvl;
			}

			return new VCycleSchedule(numLevels, path);
		}
	}
}
