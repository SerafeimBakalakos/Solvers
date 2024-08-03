namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class StochasticAnalysisRunner
	{
		/// <summary>
		/// Run a single analysis and return the responses in a Dictionary where each key is the unique name of a response and
		/// the corresponding value of type object. The keys must match the ones registered in <see cref="Responses"/>.
		/// The values will be casted, depending on how they were defined in <see cref="Responses"/>.
		/// </summary>
		/// <returns>Keys = unique names of responses. Values are abstracted or boxed in type object.</returns>
		public delegate Dictionary<string, object> RunSingleAnalysis();

		private readonly List<(RunSingleAnalysis runAnalysis, int numRepetitions, string description)> analysisGroups;

		public StochasticAnalysisRunner()
		{
			analysisGroups = new List<(RunSingleAnalysis runAnalysis, int numRepetitions, string description)>();
		}

		public bool PrintMessagesToConsole { get; set; } = true;

		public void RegisterAnalysisGroup(RunSingleAnalysis runAnalysis, int numRepetitions, string description)
		{
			analysisGroups.Add((runAnalysis, numRepetitions, description));
		}

		public List<ISingleAnalysisResponse> Responses { get; } = new List<ISingleAnalysisResponse>();

		public void RunAll()
		{
			if (analysisGroups.Count == 0)
			{
				throw new ArgumentException("At least one analysis group must be registered");
			}

			bool mustPrintEmptyLineAfterAnalysisHeader = MustPrintLineBeforeResponses(false);
			bool mustPrintEmptyLineAfterStatisticsHeader = MustPrintLineBeforeResponses(true);

			int numAnalysesTotal = 0;
			foreach ((RunSingleAnalysis runAnalysis, int numRepetitions, string description) in analysisGroups)
			{
				numAnalysesTotal += numRepetitions;
			}

			// Run each analysis
			int currentAnalysis = 0;
			foreach ((RunSingleAnalysis runAnalysis, int numRepetitions, string description) in analysisGroups)
			{
				for (int i = 0; i < numRepetitions; i++)
				{
					Print($"*************** Analysis {currentAnalysis + 1}/{numAnalysesTotal} ***************");
					if (mustPrintEmptyLineAfterAnalysisHeader)
					{
						PrintLine();
					}

					var msg = new StringBuilder();
					Dictionary<string, object> results = runAnalysis();
					foreach (ISingleAnalysisResponse response in Responses)
					{
						response.SetValueForCurrentAnalysis(results[response.Name]);
						string txt = response.ReportForAnalysis(currentAnalysis);
						msg.Append(txt);
					}

					msg.AppendLine();
					msg.AppendLine();
					Print(msg.ToString());
					currentAnalysis++;
				}
			}

			// Statistics
			var msgStats = new StringBuilder();
			msgStats.AppendLine("********************* Statistics *********************");
			msgStats.AppendLine($"Total number of analyses: {numAnalysesTotal}");

			currentAnalysis = 0;
			for (int g = 0; g < analysisGroups.Count; g++)
			{
				(_, int numRepetitions, string description) = analysisGroups[g];
				msgStats.AppendLine();
				msgStats.Append($"{g}) {description} - num analyses = {numRepetitions}");
				if (mustPrintEmptyLineAfterStatisticsHeader)
				{
					msgStats.AppendLine();
				}

				foreach (ISingleAnalysisResponse response in Responses)
				{
					string txt = response.ReportStatistics(currentAnalysis, numRepetitions);
					msgStats.Append(txt);
				}

				msgStats.AppendLine();
				currentAnalysis += numRepetitions;
			}

			Print(msgStats.ToString());
		}

		private void Print(string msg)
		{
			if (PrintMessagesToConsole)
			{
				Console.Write(msg);
			}
			else
			{
				Debug.Write(msg);
			}
		}

		private void PrintLine(string msg = "")
		{
			if (PrintMessagesToConsole)
			{
				Console.WriteLine(msg);
			}
			else
			{
				Debug.WriteLine(msg);
			}
		}

		private bool MustPrintLineBeforeResponses(bool atEnd)
		{
			if (!atEnd)
			{
				foreach (ISingleAnalysisResponse response in Responses)
				{
					if (response.DescriptionPerAnalysis != null) // first response to be printed
					{
						if (response.PrintOnSameLineAsPrevious)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				}

				return true; // Reaching this point means no responses are printed, so we can change line for the new analysis.
			}
			else
			{
				foreach (ISingleAnalysisResponse response in Responses)
				{
					if (response.DescriptionAtEnd != null) // first response to be printed
					{
						if (response.PrintOnSameLineAsPrevious)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
				}

				return true; // Reaching this point means no responses are printed, so we can change line for the new task.
			}
		}
	}
}
