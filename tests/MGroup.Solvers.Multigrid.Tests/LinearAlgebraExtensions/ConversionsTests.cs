namespace MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.LinearAlgebraExtensions.Commons;
	using MGroup.Solvers.Multigrid.Tests.LinearAlgebraExtensions.Unchanged;

	using Xunit;

	public static class ConversionsTests
	{
		[Fact]
		public static void TestConversionCsrToCsc()
		{
			var csr = CsrMatrix.CreateFromArrays(SparseRectangular10by5.NumRows, SparseRectangular10by5.NumCols,
				SparseRectangular10by5.CsrValues, SparseRectangular10by5.CsrColIndices, SparseRectangular10by5.CsrRowOffsets,
				true);

			var cscExpected = CscMatrix.CreateFromArrays(SparseRectangular10by5.NumRows, SparseRectangular10by5.NumCols,
				SparseRectangular10by5.CscValues, SparseRectangular10by5.CscRowIndices, SparseRectangular10by5.CscColOffsets,
				true);

			CscMatrix cscComputed = Conversions.CsrToCsc(csr);

			double tol = 1E-20;
			var comparer = new MatrixComparer(tol);
			comparer.AssertEqual(cscExpected, cscComputed);

			Assert.Equal(cscExpected.RawValues, cscComputed.RawValues);
			Assert.Equal(cscExpected.RawRowIndices, cscComputed.RawRowIndices);
			Assert.Equal(cscExpected.RawColOffsets, cscComputed.RawColOffsets);

		}

		[Fact]
		public static void TestConversionCsrToSymmetricCsc()
		{
			var csr = CsrMatrix.CreateFromArrays(SparsePosDef10by10.Order, SparsePosDef10by10.Order,
				SparsePosDef10by10.CsrValues, SparsePosDef10by10.CsrColIndices, SparsePosDef10by10.CsrRowOffsets,
				true);

			var symCscExpected = SymmetricCscMatrix.CreateFromArrays(SparsePosDef10by10.Order,
				SparsePosDef10by10.SymmetricCscValues, SparsePosDef10by10.SymmetricCscRowIndices, SparsePosDef10by10.SymmetricCscColOffsets,
				true);

			SymmetricCscMatrix symCscComputed = Conversions.CsrToSymmetricCsc(csr);

			double tol = 1E-20;
			var comparer = new MatrixComparer(tol);
			comparer.AssertEqual(symCscExpected, symCscComputed);

			Assert.Equal(symCscExpected.RawValues, symCscComputed.RawValues);
			Assert.Equal(symCscExpected.RawRowIndices, symCscComputed.RawRowIndices);
			Assert.Equal(symCscExpected.RawColOffsets, symCscComputed.RawColOffsets);
		}
	}
}
