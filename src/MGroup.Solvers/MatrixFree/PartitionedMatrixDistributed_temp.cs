namespace MGroup.Solvers.MatrixFree
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering;

	using MPI;

	public class PartitionedMatrixDistributed_temp : DefaultMatrix
	{
		private readonly IComputeEnvironment environment;
		private readonly ISubdomain_v2 domain;
		private readonly DistributedOverlappingMatrix<IMatrix> distributedMatrix;

		public PartitionedMatrixDistributed_temp(IComputeEnvironment environment, ISubdomain_v2 domain, DistributedOverlappingIndexer indexer)
		{
			this.environment = environment;
			this.domain = domain;
			this.NumColumns = indexer.NumGlobalIndices;

			distributedMatrix = new DistributedOverlappingMatrix<IMatrix>(indexer);
			environment.DoPerNode(elementID =>
			{
				ISuperElement element = domain.GetElement(elementID);
				IMatrix elementMatrix = element.BuildMatrix();
				distributedMatrix.LocalMatrices[elementID] = elementMatrix;
			});
		}

		public override double this[int rowIdx, int colIdx] 
		{ 
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override int NumColumns { get; }

		public override int NumRows => NumColumns;

		public override void Clear()
		{
			distributedMatrix.Clear();
		}

		public override IMatrix CreateZeroMatrixWithSameFormat()
		{
			var result = new PartitionedMatrixDistributed_temp(environment, domain, distributedMatrix.Indexer);
			return result;
		}

		public override bool HasSameFormat(IReadOnlyMatrix otherMatrix)
		{
			if (otherMatrix is PartitionedMatrixDistributed_temp casted)
			{
				if ((casted.domain == this.domain) && (casted.distributedMatrix.Indexer == this.distributedMatrix.Indexer))
				{
					return true;
					//return DictionariesHaveSameKeys(this.elementMatrices, casted.elementMatrices);
				}
			}

			return false;
		}

		public override void MultiplyIntoResult(IReadOnlyVector lhsVector, IVector rhsVector, bool transposeThis = false)
		{
			if (transposeThis)
			{
				throw new NotImplementedException();
			}

			distributedMatrix.MultiplyIntoResult(lhsVector, rhsVector);
		}
	}
}
