namespace MGroup.Solvers.MachineLearning.Tests.StochasticExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[Serializable]
	public class ResponseNumeric : SingleAnalysisResponseBase<double>
	{
		/// <summary>
		/// String format for printing the value of the response and its statistics.
		/// Since the underlying type is double, use appropriate formats (e.g. "F0" instead of "D").
		/// </summary>
		public string Format { get; set; } = "G";

		public override string ReportStatistics(int analysisIdxFirst, int numAnalyses)
		{
			if (DescriptionAtEnd == null)
			{
				return string.Empty;
			}

			int numItemsToPrint = 0;
			if (PrintSumAtEnd)
			{
				numItemsToPrint++;
			}
			if (PrintAverageAtEnd)
			{
				numItemsToPrint++;
			}
			if (PrintStdDevAtEnd)
			{
				numItemsToPrint++;
			}
			if (PrintMinMaxAtEnd)
			{
				numItemsToPrint += 2;
			}

			if ((numItemsToPrint == 0) && (IsConstant == false))
			{
				return string.Empty;
			}

			var msg = new StringBuilder();
			if (!PrintOnSameLineAsPrevious)
			{
				msg.AppendLine();
			}

			msg.Append(DescriptionAtEnd);
			if (IsConstant)
			{
				msg.Append(" = ");
				msg.Append(values[0].ToString(Format));
				AppendUnits(msg);
				msg.Append(". ");
				return msg.ToString();
			}
			else
			{
				msg.Append(": ");
			}

			var relevantValues = new double[numAnalyses];
			for (int i = 0; i < numAnalyses; i++)
			{
				relevantValues[i] = values[analysisIdxFirst + i];
			}

			double sum = relevantValues.Sum();
			if (PrintSumAtEnd)
			{
				msg.Append("sum = ");
				msg.Append(ValueToString(sum));
				AppendUnits(msg);
				numItemsToPrint--;
				if (numItemsToPrint > 0)
				{
					msg.Append(", ");
				}
			}

			double avg = sum / numAnalyses;
			if (PrintAverageAtEnd)
			{
				msg.Append("average = ");
				msg.Append(ValueToString(avg));
				AppendUnits(msg);
				numItemsToPrint--;
				if (numItemsToPrint > 0)
				{
					msg.Append(", ");
				}
			}

			if (PrintStdDevAtEnd)
			{
				double stdDev = 0.0;
				foreach (double val in relevantValues)
				{
					double z = val - avg;
					stdDev += z * z;
				}
				stdDev = Math.Sqrt(stdDev / (numAnalyses - 1));

				msg.Append("std-dev = ");
				msg.Append(ValueToString(stdDev));
				AppendUnits(msg);
				numItemsToPrint--;
				if (numItemsToPrint > 0)
				{
					msg.Append(", ");
				}
			}

			if (PrintMinMaxAtEnd)
			{
				double min = double.MaxValue;
				double max = double.MinValue;
				foreach (double val in relevantValues)
				{
					if (val < min)
					{
						min = val;
					}

					if (val > max)
					{
						max = val;
					}
				}

				msg.Append("min = ");
				msg.Append(ValueToString(min));
				AppendUnits(msg);
				msg.Append(", ");
				msg.Append("max = ");
				msg.Append(ValueToString(max));
				AppendUnits(msg);
			}

			msg.Append(". ");

			return msg.ToString();
		}

		public override void SetValueForCurrentAnalysis(object value)
		{
			if (IsConstant && (values.Count > 0))
			{
				return;
			}

			values.Add(Convert.ToDouble(value));
		}

		protected override string ValueToString(double value) => value.ToString(Format);
	}
}
