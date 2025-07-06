using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
	public class PythonCallPaths
	{
		public PythonCallPaths(string workDirectory)
		{
			var date = DateTime.Now;
			string pattern = "yyyy-MM-dd-hh-mm";
			Guid guid = Guid.NewGuid();
			TempSubdirectoryNameOnly = $"_temp_{date.ToString(pattern)}_{guid}";
			TempSubdirectoryFullPath = $"{workDirectory}\\{TempSubdirectoryNameOnly}";
			InputsArrays = TempSubdirectoryFullPath + "\\inputs_arrays.json";
			InputsSerialized = TempSubdirectoryFullPath + "\\inputs_serialized.json";
			OutputsArrays = TempSubdirectoryFullPath + "\\outputs_arrays.son";
			OutputsSerialized = TempSubdirectoryFullPath + "\\outputs_serialized.json";
			LogPerformance = TempSubdirectoryFullPath + "\\log_performance.json";
			LogErrors = TempSubdirectoryFullPath + "\\log_errors.json";
		}

		public string InputsArrays { get; }

		public string InputsSerialized { get; }

		public string LogErrors { get; }

		public string LogPerformance { get; }

		public string OutputsArrays { get; }

		public string OutputsSerialized { get; }

		public string TempSubdirectoryFullPath { get; }
		public string TempSubdirectoryNameOnly { get; }

		public string MakePathForArray(string name) => $"{TempSubdirectoryFullPath}\\{name}.npy";
	}
}
