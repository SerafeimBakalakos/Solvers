namespace MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary.CSR
{
	using System;
	using System.Collections.Generic;

	using MGroup.LinearAlgebra.Commons;
	using MGroup.LinearAlgebra.Exceptions;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.Iterative.Stationary;
	using MGroup.Solvers.LinearAlgebraExtensions.Implementations.Managed;
	using MGroup.LinearAlgebra.Reduction;

	public abstract class CsrStationaryIterationBase : IStationaryIteration, IDisposable
	{
		private readonly static CsrStationaryIterationCache cache = new();

		protected readonly ManagedStationaryIterationProvider provider;
		protected CsrMatrix? matrix;
		protected int[]? diagonalOffsets;

		public CsrStationaryIterationBase()
		{
			provider = new ManagedStationaryIterationProvider();
		}

		public void Dispose()
		{
			if (matrix is CsrMatrix csr)
			{
				cache.Unregister(csr, this);
				matrix = null;
				diagonalOffsets = null;
			}
		}

		public virtual void UpdateMatrix(IReadOnlyMatrix matrix, bool isPatternModified)
		{
			Preconditions.CheckSquare(matrix);
			if (matrix is CsrMatrix csrMatrix)
			{
				if (isPatternModified)
				{
					if (this.matrix is CsrMatrix previousMatrix)
					{
						cache.Unregister(previousMatrix, this);
						diagonalOffsets = null;
					}
				}

				this.matrix = csrMatrix;
				diagonalOffsets = cache.GetOrCreateDiagOffsets(csrMatrix, 
					() => provider.LocateDiagonalOffsetsCsr(csrMatrix.NumRows, csrMatrix.RawRowOffsets, csrMatrix.RawColIndices));
				cache.Register(csrMatrix, this);
			}
			else
			{
				throw new InvalidSparsityPatternException(GetType().Name + " can be used only for matrices in CSR format.");
			}
		}

		public abstract string Name { get; }

		public abstract IStationaryIteration CopyWithInitialSettings();

		public abstract void Execute(Vector rhs, Vector solution);
	}
}
