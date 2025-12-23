namespace MGroup.Solvers.DDM.Tests._temp
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient;
	using MGroup.LinearAlgebra.Iterative.PreconditionedConjugateGradient.Logging;

	public class PcgDebugLogger_v2 : IPcgLogger
	{
		private int iteration = 0;

		public void Clear()
		{
			iteration = 0;
		}

		public void Log(PcgAlgorithmBase pcg)
		{
			double normAd = (pcg.MatrixTimesDirection != null) ? pcg.MatrixTimesDirection.Norm2() : 0;
			Debug.WriteLine($"iteration {iteration}: " +
				$"b = {pcg.ParamBeta}, " +
				$"norm(d) = {pcg.Direction.Norm2()}, " +
				$"norm(A*d) = {normAd}, " +
				$"a = {pcg.StepSize}, " +
				$"norm(x) = {pcg.Solution.Norm2()}, " +
				$"norm(r) = {pcg.Residual.Norm2()}, " +
				$"norm(s) = {pcg.PrecondResidual.Norm2()}");
			iteration++;
		}

		public string Report() => "Nothing to report";
	}
}
