namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public abstract class SingleAnalysisResponseBase<T> : ISingleAnalysisResponse
	{
		protected readonly List<T> values = new List<T>();
		protected T tabooValue;
		protected bool useTabooValue = false;

		public string Name { get; set; }

		//public int ID { get; set; } = -1;

		public bool IsConstant { get; set; } = false;

		public string UnitsDescription { get; set; }

		public bool PrintOnSameLineAsPrevious { get; set; } = false;

		/// <summary>
		/// If null, no description will be printed per analysis.
		/// </summary>
		public string DescriptionPerAnalysis { get; set; }

		/// <summary>
		/// If null, no description will be printed at the end.
		/// </summary>
		public string DescriptionAtEnd { get; set; }

		public bool PrintSumAtEnd { get; set; } = false;

		public bool PrintAverageAtEnd { get; set; } = false;

		public bool PrintStdDevAtEnd { get; set; } = false;

		public bool PrintMinMaxAtEnd { get; set; } = false;

		public object ValueToIgnoreWhenPrinting 
		{ 
			set
			{
				this.tabooValue = (T)value;
				this.useTabooValue = true;
			}
		}

		public string ReportForAnalysis(int analysisIdx)
		{
			T val = GetValue(analysisIdx);
			if (useTabooValue && val.Equals(tabooValue))
			{
				return string.Empty;
			}

			if (DescriptionPerAnalysis == null)
			{
				return string.Empty;
			}

			var msg = new StringBuilder();
			if (!PrintOnSameLineAsPrevious)
			{
				msg.AppendLine();
			}

			msg.Append(DescriptionPerAnalysis);
			msg.Append(" = ");
			msg.Append(ValueToString(val));
			AppendUnits(msg);
			msg.Append(". ");

			return msg.ToString();
		}

		public virtual void SetValueForCurrentAnalysis(object value)
		{
			if (IsConstant && (values.Count > 0))
			{
				return;
			}

			values.Add((T)value);
		}

		protected void AppendUnits(StringBuilder msg)
		{
			if (UnitsDescription != null)
			{
				msg.Append(' ');
				msg.Append(UnitsDescription);
			}
		}

		protected T GetValue(int analysisIdx)
		{
			if (IsConstant)
			{
				if (values.Count == 0)
				{
					throw new InvalidOperationException($"No responses '{Name}' have been saved.");
				}

				return values[0];
			}
			else
			{
				if (analysisIdx >= values.Count)
				{
					throw new InvalidOperationException(
						$"The response '{Name}' for analysis no. {analysisIdx} has not been saved.");
				}

				return values[analysisIdx];
			}
		}

		protected virtual string ValueToString(T value) => value.ToString();

		public abstract string ReportStatistics(int analysisIdxFirst, int numAnalyses);
	}
}
