namespace MGroup.Solvers.LinearAlgebraExtensions.Distributed
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;

	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Vectors;

	public static class DistributedOverlappingVectorExtensions
	{
		//TODOMPI: A ReduceOverlappingEntries(IReduction), which would cover sum and regularization would be more useful. 
		//      However the implementation should not be slower than the current SumOverlappingEntries(), since that is a very
		//      important operation.
		//TODOMPI: Test this
		/// <summary>
		/// Gathers the entries of remote vectors that correspond to the boundary entries of the local vectors and regularizes 
		/// them, meaning each of these entries is divided via the sum of corresponding entries over all local vectors. 
		/// Therefore, the resulting local vectors will not have the same values at their corresponding overlapping entries.
		/// </summary>
		/// <remarks>
		/// Requires communication between compute nodes:
		/// Each compute node sends its boundary entries to the neighbors that are assiciated with these entries. 
		/// Each neighbor receives only the entries it has in common.
		/// </remarks>
		public static void RegularizeOverlappingEntries_v2(this DistributedOverlappingVector vector)
		{
			// Sum the values of overlapping entries in a different vector.
			DistributedOverlappingVector reducedVector = vector.CopyAsDistributed();
			reducedVector.SumOverlappingEntries();

			// Divide the values of overlapping entries via their sums.
			Action<int> regularizeLocalVectors = nodeID =>
			{
				Vector orginalLocalVector = vector.LocalVectors[nodeID];
				Vector reducedLocalVector = reducedVector.LocalVectors[nodeID];
				int numLocalIndices = vector.Indexer.GetNumLocalIndices(nodeID);
				for (int i = 0; i < numLocalIndices; ++i)
				{
					orginalLocalVector[i] /= reducedLocalVector[i];
				}
			};
			vector.Environment.DoPerNode(regularizeLocalVectors);
		}
	}
}
