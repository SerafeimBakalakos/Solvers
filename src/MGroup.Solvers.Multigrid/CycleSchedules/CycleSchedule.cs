namespace MGroup.Solvers.Multigrid.CycleSchedules
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	//TODO: Refactor this. It must be as simple as possible for the user to choose it, but also stay relatively immutable, at least the path.
	public enum CycleSchedule
	{
		VCycle, WCycle, FCycle
	}
}
