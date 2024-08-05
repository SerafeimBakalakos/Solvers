namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using MGroup.Solvers.MachineLearning.StochasticExtensions.RandomNumberGeneration;

	public class StochasticAnalysisRunner
	{
		private readonly List<(int numRepetitions, string description)> analysisGroups;
		private readonly IAutoStochasticAnalysis stochasticAnalysis;

		private RepeatableRandom rng;
		private string directoryToSaveOrLoad = null;
		private int numAnalysesToSaveOrLoad = 0;
		private bool loadFirstAnalyses = false;
		private bool saveFirstAnalyses = false;

		public StochasticAnalysisRunner(IAutoStochasticAnalysis stochasticAnalysis, int? rngSeed = null)
		{
			analysisGroups = new List<(int numRepetitions, string description)>();
			this.stochasticAnalysis = stochasticAnalysis;
			this.rng = rngSeed.HasValue ? new RepeatableRandom(rngSeed.Value) : new RepeatableRandom();
		}

		public bool PrintMessagesToConsole { get; set; } = true;

		public void RegisterAnalysisGroup(int numRepetitions, string description)
		{
			analysisGroups.Add((numRepetitions, description));
		}

		public List<ISingleAnalysisResponse> Responses { get; } = new List<ISingleAnalysisResponse>();

		public void RunAll()
		{
			if (analysisGroups.Count == 0)
			{
				throw new ArgumentException("At least one analysis group must be registered");
			}

			stochasticAnalysis.InitializeModel(rng);
			stochasticAnalysis.InitializeSolver();

			bool mustPrintEmptyLineAfterAnalysisHeader = MustPrintLineBeforeResponses(false);
			bool mustPrintEmptyLineAfterStatisticsHeader = MustPrintLineBeforeResponses(true);

			int numAnalysesTotal = 0;
			foreach ((int numRepetitions, string description) in analysisGroups)
			{
				numAnalysesTotal += numRepetitions;
			}

			// Run each analysis
			int currentAnalysis = 0;
			foreach ((int numRepetitions, string description) in analysisGroups)
			{
				for (int i = 0; i < numRepetitions; i++)
				{
					Print($"*************** Analysis {currentAnalysis + 1}/{numAnalysesTotal} ***************");
					if (mustPrintEmptyLineAfterAnalysisHeader)
					{
						PrintLine();
					}

					RunSingleAnalysis(currentAnalysis);

					var msg = new StringBuilder();
					foreach (ISingleAnalysisResponse response in Responses)
					{
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
				(int numRepetitions, string description) = analysisGroups[g];
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

		/// <summary>
		/// Cancels the effects of <see cref="SaveFirstAnalyses(int)"/>.
		/// </summary>
		public void LoadFirstAnalyses(int numAnalysesToLoad, string loadDirectory)
		{
			saveFirstAnalyses = false;
			loadFirstAnalyses = true;
			numAnalysesToSaveOrLoad = numAnalysesToLoad;
			directoryToSaveOrLoad = loadDirectory;
		}

		/// <summary>
		/// Cancels the effects of <see cref="LoadFirstAnalyses(int)"/>.
		/// </summary>
		public void SaveFirstAnalyses(int numAnalysesToSave, string saveDirectory)
		{
			saveFirstAnalyses = true;
			loadFirstAnalyses = false;
			numAnalysesToSaveOrLoad = numAnalysesToSave;
			directoryToSaveOrLoad = saveDirectory;
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

		private void RunSingleAnalysis(int currentAnalysis)
		{
			if (loadFirstAnalyses && (currentAnalysis < numAnalysesToSaveOrLoad))
			{
				// Preload all saved analyses the first time. Then do nothing.
				if (currentAnalysis == 0)
				{
					LoadState();
				}
			}
			else
			{
				Dictionary<string, object> results = stochasticAnalysis.RunSingleAnalysis(currentAnalysis);
				foreach (ISingleAnalysisResponse response in Responses)
				{
					response.SetValueForCurrentAnalysis(results[response.Name]);
				}

				if (saveFirstAnalyses && (currentAnalysis == numAnalysesToSaveOrLoad - 1))
				{
					SaveState();
				}
			}
		}

		private void LoadState()
		{
			string pathSerialized = Path.Combine(directoryToSaveOrLoad, "serialized_stochastic_runner");
			if (!File.Exists(pathSerialized))
			{
				throw new IOException($"Invalid file: {pathSerialized}");
			}

			stochasticAnalysis.LoadState();

			State state = null;
			using (Stream stream = File.Open(pathSerialized, FileMode.Open))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				state = (State)(binaryFormatter.Deserialize(stream));
			}

			this.Responses.Clear();
			this.Responses.AddRange(state.Responses);
			this.rng.LoadState(state.RngState);
		}

		private void SaveState()
		{
			if (!Directory.Exists(directoryToSaveOrLoad))
			{
				throw new IOException($"Invalid directory: {directoryToSaveOrLoad}");
			}

			var state = new State
			{
				Responses = this.Responses,
				RngState = rng.ExtractState(),
			};

			string pathSerialized = Path.Combine(directoryToSaveOrLoad, "serialized_stochastic_runner");
			using (Stream stream = File.Open(pathSerialized, FileMode.Create))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(stream, state);
			}

			stochasticAnalysis.SaveState();
		}

		[Serializable]
		private class State
		{
			public List<ISingleAnalysisResponse> Responses { get; set; }

			public RepeatableRandom.State RngState { get; set; }
		}
	}
}
