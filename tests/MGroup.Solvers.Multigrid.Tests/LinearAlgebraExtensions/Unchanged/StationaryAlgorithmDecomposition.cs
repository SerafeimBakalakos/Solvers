namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	internal class StationaryAlgorithmDecomposition
	{
		private readonly SparseMatrix original;
		private readonly Dictionary<string, SparseMatrix> combinations;
		private SparseMatrix D;
		private SparseMatrix L;
		private SparseMatrix U;

		public StationaryAlgorithmDecomposition(SparseMatrix original)
		{
			this.original = original;
			this.combinations = new Dictionary<string, SparseMatrix>();
		}

		public SparseMatrix GetD()
		{
			if (D == null)
			{
				D = original.ExtractDiagonal();
			}

			return D;
		}

		public SparseMatrix GetL()
		{
			if (L == null)
			{
				L = original.ExtractLowerTriangle();
			}

			return L;
		}

		public SparseMatrix GetU()
		{
			if (U == null)
			{
				U = original.ExtractUpperTriangle();
			}

			return U;
		}

		public SparseMatrix GetCombination(string name, double coeffL, double coeffD, double coeffU)
		{
			bool exists = combinations.TryGetValue(name, out SparseMatrix result);
			if (!exists)
			{
				result = new SparseMatrix(original.NumRows, original.NumColumns);
				if (coeffL != 0)
				{
					result = coeffL * GetL() + result;
				}

				if (coeffD != 0)
				{
					result = coeffD * GetD() + result;
				}

				if (coeffU != 0)
				{
					result = coeffU * GetU() + result;
				}
			}

			return result;
		}
	}
}
