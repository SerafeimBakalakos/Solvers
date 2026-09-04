namespace MGroup.Solvers.Multigrid.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Reflection.Emit;
	using System.Text;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class MultigridSmoothers
	{
		private readonly int numLevels;
		private readonly IMultigridSmoother[] preSmoothers;
		private readonly IMultigridSmoother[] postSmoothers;

		public MultigridSmoothers(int numLevels)
		{
			this.numLevels = numLevels;
			preSmoothers = new IMultigridSmoother[numLevels - 1];
			postSmoothers = new IMultigridSmoother[numLevels - 1];
		}

		public void ApplyPreSmoothing(int level, Vector rhs, Vector solution) => preSmoothers[level].Apply(rhs, solution);

		public void ApplyPostSmoothing(int level, Vector rhs, Vector solution) => postSmoothers[level].Apply(rhs, solution);

		public void DefineSmoother(IMultigridSmoother smoother)
		{
			for (int lvl = 0; lvl < numLevels; lvl++)
			{
				IMultigridSmoother lvlSmoother = smoother.DeepCopy();
				preSmoothers[lvl] = lvlSmoother;
				postSmoothers[lvl] = lvlSmoother;
			}
		}

		public void DefineSmootherPerLevel(int level, IMultigridSmoother smoother)
		{
			preSmoothers[level] = smoother;
			postSmoothers[level] = smoother;
		}

		public void DefineSmoothersPerLevel(int level, IMultigridSmoother preSmoother, IMultigridSmoother postSmoother)
		{
			preSmoothers[level] = preSmoother;
			postSmoothers[level] = postSmoother;
		}

		public void Update(int level, IReadOnlyMatrix matrix, bool areDofsModified)
		{
			preSmoothers[level].UpdateMatrix(matrix, areDofsModified);
			if (postSmoothers[level] != preSmoothers[level])
			{
				postSmoothers[level].UpdateMatrix(matrix, areDofsModified);
			}
		}
	}
}
