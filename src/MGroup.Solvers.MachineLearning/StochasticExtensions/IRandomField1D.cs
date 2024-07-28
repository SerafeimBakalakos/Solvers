namespace MGroup.Solvers.MachineLearning.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IRandomField1D
	{
		public double CalcValueAt(double coords);

		public void Initialize();

		/// <summary>
		/// Generates a new realization of the random field and returns the parameters that describe it. 
		/// </summary>
		/// <returns></returns>
		public double[] Regenerate();
	}
}
