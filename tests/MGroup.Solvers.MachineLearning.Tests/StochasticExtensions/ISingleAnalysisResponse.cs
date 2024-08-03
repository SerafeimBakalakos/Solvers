namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public interface ISingleAnalysisResponse
	{
		public string Name { get; set; }

		//public int ID { get; set; }

		public bool IsConstant { get; set; }

		public string UnitsDescription { get; set; }

		public bool PrintOnSameLineAsPrevious { get; set; }

		/// <summary>
		/// If null, no description will be printed per analysis.
		/// </summary>
		public string DescriptionPerAnalysis { get; set; }

		/// <summary>
		/// If null, no description will be printed at the end.
		/// </summary>
		public string DescriptionAtEnd { get; set; }

		public bool PrintMinMaxAtEnd { get; set; }

		public bool PrintAverageAtEnd { get; set; }

		public bool PrintStdDevAtEnd { get; set; }

		public object ValueToIgnoreWhenPrinting { set; }

		public string ReportForAnalysis(int analysisIdx);

		public string ReportStatistics(int analysisIdxFirst, int numAnalyses);

		public void SetValueForCurrentAnalysis(object value);
	}
}
