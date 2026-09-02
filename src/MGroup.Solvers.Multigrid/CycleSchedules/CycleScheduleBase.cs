namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public abstract class CycleScheduleBase : ICycleSchedule
	{
		/// <summary>
		/// The total number of multigrid levels
		/// </summary>
		protected readonly int numLevels;

		/// <summary>
		/// The total number of grids visited during one cycle
		/// </summary>
		protected readonly int numSteps;

		/// <summary>
		/// Contains the full progression of levels
		/// </summary>
		protected readonly int[] path;

		protected int currentStep;

		protected CycleScheduleBase(int numLevels, int[] path)
		{
			this.numLevels = numLevels;
			this.path = path;
			numSteps = path.Length;
			currentStep = 0;
		}

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

		public void Reset() => currentStep = 0;
	}
}
