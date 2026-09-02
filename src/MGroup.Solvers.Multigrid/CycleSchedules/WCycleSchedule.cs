namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Text;

	/// <summary>
	/// Implements the level progression for W-cycles.
	/// </summary>
	public class WCycleSchedule : CycleScheduleBase
	{
		private WCycleSchedule(int numLevels, int[] path)
			: base(numLevels, path)
		{
		}

		/// <summary>
		/// Constructs an instance of this class.
		/// </summary>
		/// <param name="numLevels">The total number of multigrid levels, including the finest and coarsest grids.</param>
		public static WCycleSchedule Create(int numLevels)
		{
			//int stepsUpperBound = (int)Math.Pow(2, numLevels + 1); // This is huge and too strict
			//var paths = new List<int>(stepsUpperBound);
			var path = new List<int>(2 * numLevels); // It will definetly be resized.
			var descents = new int[numLevels]; // How many times we've gone down from each level
			int lvl = 0; // Start at top level (finest)
			path.Add(lvl);

			while (true)
			{
				if (lvl == numLevels - 1) // Coarsest: must go up
				{
					lvl--;
					path.Add(lvl);
				}
				else if (descents[lvl] < 2) // Go down twice from each level in a W-cycle
				{
					descents[lvl]++;
					lvl++;
					path.Add(lvl);
				}
				else // Done with this level during this pass. Reset and go up.
				{
					descents[lvl] = 0;
					lvl--;
					path.Add(lvl);

					if (lvl == 0)
					{
						break; // Cycle ends here.
					}
				}
			}

			return new WCycleSchedule(numLevels, path.ToArray());
		}
	}
}
