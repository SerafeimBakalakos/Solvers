namespace MGroup.Solvers.Multigrid.Algorithms
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Vectors;

	public class MultigridVectorHierarchy
	{
		private Vector[] lhsVectors = [];
		private Vector[] rhsVectors = [];
		private Vector[] workVectors = [];

		public void AllocateVectors(IMultigridOperatorHierarchy operatorHierarchy)
		{
			int numLevels = operatorHierarchy.NumLevels;
			if (workVectors.Length != numLevels)
			{
				lhsVectors = new Vector[numLevels];
				rhsVectors = new Vector[numLevels];
				workVectors = new Vector[numLevels];

				// The finest level gets its rhs, lhs vector directly from the consumer
				int systemSize = operatorHierarchy.GetSystemMatrix(0).NumRows;
				workVectors[0] = Vector.CreateZero(systemSize);

				// For the other levels allocate all vectors
				for (int lvl = 1; lvl < numLevels; lvl++)
				{
					systemSize = operatorHierarchy.GetSystemMatrix(lvl).NumRows;
					lhsVectors[lvl] = Vector.CreateZero(systemSize);
					rhsVectors[lvl] = Vector.CreateZero(systemSize);
					workVectors[lvl] = Vector.CreateZero(systemSize);
				}
			}
			else
			{
				// The finest level gets its rhs, lhs vector directly from the consumer
				int systemSize = operatorHierarchy.GetSystemMatrix(0).NumRows;
				if (workVectors[0].Length != systemSize)
				{
					workVectors[0] = Vector.CreateZero(systemSize);
				}

				// For the other levels allocate/clear the rhs, lhs vectors
				for (int lvl = 1; lvl < numLevels; lvl++)
				{
					systemSize = operatorHierarchy.GetSystemMatrix(lvl).NumRows;
					if (workVectors[lvl].Length != systemSize)
					{
						lhsVectors[lvl] = Vector.CreateZero(systemSize);
						rhsVectors[lvl] = Vector.CreateZero(systemSize);
						workVectors[lvl] = Vector.CreateZero(systemSize);
					}
					//else // These are probably redundant since they are overwritten or cleared during each cycle.
					//{
					//	lhsVectors[lvl].Clear();
					//	rhsVectors[lvl].Clear();
					//}
				}
			}
		}

		public void ClearExceptFinestLevel(bool alsoClearFinestLevel)
		{
			int numLevels = workVectors.Length;
			int lvl = alsoClearFinestLevel ? 0 : 1;
			for (; lvl < numLevels; lvl++)
			{
				lhsVectors[lvl].Clear();
				rhsVectors[lvl].Clear();
			}
		}

		public Vector GetLhsVector(int level) => lhsVectors[level];

		public Vector GetRhsVector(int level) => rhsVectors[level];

		public Vector GetWorkVector(int level) => workVectors[level];

		public void SetFinestLevelVectors(Vector rhsVector, Vector solution)
		{
			rhsVectors[0] = rhsVector;
			lhsVectors[0] = solution;
		}
	}
}
