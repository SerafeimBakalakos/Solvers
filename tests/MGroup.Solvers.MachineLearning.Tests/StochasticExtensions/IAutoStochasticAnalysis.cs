namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public interface IAutoStochasticAnalysis
	{
		public void InitializeModel();

		public void InitializeSolver();

		public void LoadState();

		/// <summary>
		/// Run a single analysis and return the responses in a Dictionary where each key is the unique name of a response and
		/// the corresponding value of type object. The keys must match the ones registered in <see cref="Responses"/>.
		/// The values will be casted, depending on how they were defined in <see cref="Responses"/>.
		/// </summary>
		/// <param name="analysisId">The contiguous 0-based ID of the analysis.</param>
		/// <returns>Keys = unique names of responses. Values are abstracted or boxed in type object.</returns>
		public Dictionary<string, object> RunSingleAnalysis(int analysisId);

		public void SaveState();
	}
}
