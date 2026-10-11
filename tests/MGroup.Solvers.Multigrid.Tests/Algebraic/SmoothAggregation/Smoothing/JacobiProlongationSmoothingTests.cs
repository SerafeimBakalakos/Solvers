namespace MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.Smoothing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.Solvers.LinearAlgebraExtensions.EigenSystems;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices;
	using MGroup.Solvers.LinearAlgebraExtensions.Matrices.Builders;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Smoothing;
	using MGroup.Solvers.Multigrid.Tests.Algebraic.SmoothAggregation.NearNullspaceCandidates;
	using MGroup.Solvers.Multigrid.Tests.Examples;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	using Xunit;

	public static class JacobiProlongationSmoothingTests
	{
		public static CsrMatrix GetProlongationPoisson1D(double weightFactor, int degree)
		{
			if (Math.Abs(weightFactor - 4.0/3.0) < 1E-12 && (degree == 1))
			{
				double[] values = [0.44721359549995787, 0.43946231946747427, 0.16330447332925876, 0.30330991435496096, 0.3537823729748982, 0.5728663259698752, 0.40843358003967656, 0.2042742916157737, 0.22089943129809275, 0.3935208882426855, 0.5764931820377706, .3730851989761724, 0.26200611704501664, 0.18273784754466882, 0.43894086379344954, 0.5432268712127546, 0.28580563115540425];
				int[] colIndices = [0, 0, 1, 0, 1, 1, 1, 2, 1, 2, 2, 2, 3, 2, 3, 3, 3];
				int[] rowOffsets = [0, 1, 3, 5, 6, 8, 10, 11, 13, 15, 16, 17];
				return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
			}
			else if (Math.Abs(weightFactor - 1.2) < 1E-12 && (degree == 3)) 
			{
				double[] values = [0.25029461106457573, 0.069096172368922, 0.3352264053752359, 0.19034780985786717, 0.2492749040616434, 0.3357416658606365, 0.01712475896002844, 0.11009004712085854, 0.4164203560136139, 0.08232169834486885, 0.02542713100327549, 0.36427618782365917, 0.2145200095133144, 0.22360852907689527, 0.3575116313960595, 0.0219645436778127, 0.08758681214223488, 0.41898190473178953, 0.10007151718617513, 0.0185184806441649, 0.3468417367034566, 0.24281028675493022, 0.20208285104515578, 0.37209494128251186, 0.07540750579467398, 0.38677780839988496, 0.01531931192772393, 0.24387999747655054];
				int[] colIndices = [0, 1, 0, 1, 0, 1, 2, 0, 1, 2, 0, 1, 2, 1, 2, 3, 1, 2, 3, 1, 2, 3, 2, 3, 2, 3, 2, 3];
				int[] rowOffsets = [0, 2, 4, 7, 10, 13, 16, 19, 22, 24, 26, 28];
				return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		[Theory]
		[InlineData(4.0 / 3.0, 1, 1E-14)]
		[InlineData(1.2, 3, 1E-14)]
		public static void TestForPoisson1D(double weightFactor, int degree, double tolerance)
		{
			int numElements = 12;
			(DokRowMajor dokA, Vector b) = Poisson1DProblem.CreateWithConstantSource(numElements, source: 1.0);
			CsrMatrix csrA = dokA.BuildCsrMatrix(sortColsOfEachRow: true);

			CsrMatrix tentativeP = CandidateVectorFittingTests.GetTentativeProlongationPoisson1D(relaxationStrategy: "SGS-4");

			var smoothing = new JacobiProlongationSmoothing(weightFactor, degree);
			CsrMatrix computedP = smoothing.SmoothProlongationMatrix(tentativeP, csrA);

			CsrMatrix expectedP = GetProlongationPoisson1D(weightFactor, degree);
			AssertUtilities.AssertEqualCsr(expectedP, computedP, tolerance);
		}
	}
}
