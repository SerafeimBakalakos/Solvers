namespace MGroup.Solvers.DDM.LinearSystem
{
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearSystem;

	public class SubdomainLinearSystem_v2<TMatrix> : ISubdomainLinearSystem_v2
		where TMatrix : class, IMatrix
	{
		private readonly LinearSystem_v2 fullLinearSystem;

		public SubdomainLinearSystem_v2(LinearSystem_v2 fullLinearSystem, int subdomainID)
		{
			this.SubdomainID = subdomainID;
			this.fullLinearSystem = fullLinearSystem;
		}

		IMatrix ISubdomainLinearSystem_v2.Matrix => this.Matrix;

		public TMatrix Matrix
		{
			get => ((DistributedOverlappingMatrix<TMatrix>)fullLinearSystem.Matrix).LocalMatrices[SubdomainID];
		}

		public Vector RhsVector 
		{
			get => ((DistributedOverlappingVector)fullLinearSystem.RhsVector).LocalVectors[SubdomainID];
		}

		public Vector Solution 
		{
			get => ((DistributedOverlappingVector)fullLinearSystem.Solution).LocalVectors[SubdomainID];
			set => ((DistributedOverlappingVector)fullLinearSystem.Solution).LocalVectors[SubdomainID] = value;
		}

		public int SubdomainID { get; }
	}
}
