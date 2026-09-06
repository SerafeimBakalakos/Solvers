namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class LevelProgression
	{
		/// <summary>
		/// The total number of multigrid levels
		/// </summary>
		private readonly int numLevels;

		/// <summary>
		/// The total number of grids visited during one cycle
		/// </summary>
		private readonly int numSteps;

		/// <summary>
		/// Contains the full progression of levels
		/// </summary>
		private readonly int[] path;

		private int currentStep;

		public LevelProgression(int numLevels, int[] path)
		{
			this.numLevels = numLevels;
			this.path = path;
			numSteps = path.Length;
			currentStep = 0;
		}

		/// <summary>
		/// Finds the direction for the next level in the schedule or the end of the current cycle. 
		/// </summary>
		/// <returns>
		/// +1 for moving to lvl+1 (coarser) OR -1 for moving to lvl-1 (finer) OR 0 if the current level is the final one of the cycle.
		/// </returns>
		public int MoveNext()
		{
			if (currentStep < numSteps - 1)
			{
				int currentLvl = path[currentStep];
				int nextLvl = path[currentStep + 1];
				int direction = Math.Sign(nextLvl - currentLvl);
				currentStep++;
				return direction;
			}
			else
			{
				int direction = 0;
				currentStep = 0; // Prepare for next cycle
				return direction;
			}
		}

		/// <summary>
		/// Reset to the start of the first cycle.
		/// </summary>
		public void Reset() => currentStep = 0;

	}
}
