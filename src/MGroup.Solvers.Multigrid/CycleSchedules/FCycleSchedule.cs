namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Text;

	/// <summary>
	/// Implements the level progression for F-cycles.
	/// </summary>
	public class FCycleSchedule : CycleScheduleBase
	{
		private FCycleSchedule(int numLevels, int[] path)
			: base(numLevels, path)
		{
		}

		/// <summary>
		/// Constructs an instance of this class.
		/// </summary>
		/// <param name="numLevels">The total number of multigrid levels, including the finest and coarsest grids.</param>
		public static FCycleSchedule Create(int numLevels)
		{
			int numInvertedVcycles = numLevels - 2;
			int numStepsUp = (numLevels - 1) * numLevels / 2; // per upward sweep: 0, 1, 2, ..., (numLevels-2).
			int numStepsDown = numStepsUp; // Each downwards step has a corrending upward step between the same levels.
			int numSteps = numStepsUp + numStepsDown + 1; // The last step was not counted in the upward/downward sweeps.

			// Top level (finest)
			var path = new int[numSteps];
			int s = 0;
			path[s] = 0; 

			// Initial full downward sweep: (finest, coarsest]
			for (int lvl = 1; lvl < numLevels; lvl++)
			{
				s++;
				path[s] = lvl;
			}

			// Progressively larger inverted V cycles
			for (int i = 1; i <= numInvertedVcycles; i++)
			{
				int bottom = numLevels - 1;
				int top = bottom - i;
				for (int lvl = bottom - 1; lvl >= top; lvl--) // Upward sweep: (coarsest, currentTop] 
				{
					s++;
					path[s] = lvl;
				}
				for (int lvl = top + 1; lvl <= bottom; lvl++) // Downward sweep: (currentTop, coarsest]
				{
					s++;
					path[s] = lvl;
				}
			}

			// Final full upward sweep: (coarsest, finest]
			for (int lvl = numLevels - 2; lvl >= 0; lvl--)
			{
				s++;
				path[s] = lvl;
			}

			return new FCycleSchedule(numLevels, path);
		}
	}
}
