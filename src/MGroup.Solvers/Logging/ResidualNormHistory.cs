namespace MGroup.Solvers.Logging
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class ResidualNormHistory
	{
		private readonly List<double> residuals = new List<double>(2);

		public bool IsInitialResidualStored { get; private set; } = false;

		public int NumSolverIterations { get; private set; }

		public double[] GetAllResiduals() => residuals.ToArray();

		public double[] GetResidualsExceptInitial()
		{
			if (IsInitialResidualStored)
			{
				return residuals.ToArray();
			}
			else
			{
				var result = new double[NumSolverIterations];
				for (int i = 0; i < result.Length; i++)
				{
					result[i] = residuals[i + 1];
				}

				return result;
			}
		}

		public void LogInitialResidual(double residualNorm)
		{
			if (residuals.Count > 0)
			{
				throw new InvalidOperationException("Cannot log the initial residual at this point, since others have been logged already.");
			}

			residuals.Add(residualNorm);
			IsInitialResidualStored = true;
		}

		public void LogResidual(double residualNorm)
		{
			residuals.Add(residualNorm);
			NumSolverIterations++;
		}
	}
}
