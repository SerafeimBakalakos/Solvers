namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[Serializable]
	public class ResponseString : SingleAnalysisResponseBase<string>
	{
		public override string ReportStatistics(int analysisIdxFirst, int numAnalyses) => string.Empty;
	}
}
