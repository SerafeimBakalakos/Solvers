using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
    public class PythonCaller
    {
		private readonly string workDirectory;
		private readonly string pythonInterpreter;
		private readonly string pythonScript;
		private readonly JsonSerializer jsonSerializer = new JsonSerializer();

		private IArrayFileIO arrayIO = new ArrayTextFileIO(' ');

		private readonly StringBuilder inputsTemplate = new StringBuilder();
		private readonly List<string> inputsNames = new List<string>();
		private readonly Dictionary<string, string> smallInputs = new Dictionary<string, string>();
		private readonly Dictionary<string, double[]> arrayInputsFloat64 = new Dictionary<string, double[]>();

		public PythonCaller(string workDirectory, string pythonInterpreter, string pythonScript)
		{
			this.workDirectory = workDirectory;
			this.pythonInterpreter = pythonInterpreter;
			this.pythonScript = pythonScript;
		}

		/// <summary>
		/// True (default) to delete any files created by this class. False to retain the files for manual inspection.
		/// </summary>
		public bool CleanupIOFiles { get; set; } = true;

		/// <summary>
		/// Specifies the milliseconds to wait before aborting the call to a Python script. 
		/// Indefinite waiting if <see cref="TimeoutMilliseconds"/> &lt; 0 (default).
		/// </summary>
		public int TimeoutMilliseconds { get; set; } = -1;

		/// <summary>
		/// If true (default), arrays will be transfered between C# and Python using the binary .npy format. These are not 
		/// readable by humans, but are more efficient.
		/// If false , text files (.txt extension) will be used instead, where each array entry is separated by a single 
		/// whitespace char and each row (for 2D arrays) by a newline char. These are readable by humans, but less efficient.
		/// </summary>
		public bool UseBinaryIOFilesForArrays
		{
			get => arrayIO is ArrayBinaryFileIO;
			set => arrayIO = value ? new ArrayBinaryFileIO() : new ArrayTextFileIO();
		}

		public void PassSmallInput<T>(string name, string value)
		{
			smallInputs[name] = value;
		}

		public void PassArrayInput(string name, double[] array)
		{
			arrayInputsFloat64[name] = array;
		}

		public void DescribeSmallInput(string name)
		{
			int inputIdx = inputsNames.Count;
			inputsNames.Add(name);
			if (inputsTemplate.Length != 0)
			{
				inputsTemplate.Append(',');
			}
			inputsTemplate.Append($"\"{name}\":{inputIdx}"); // placeholder
		}

		public void DescribeArrayInput(string name)
		{
			int inputIdx = inputsNames.Count;
			inputsNames.Add(name);
			if (inputsTemplate.Length != 0)
			{
				inputsTemplate.Append(',');
			}
			inputsTemplate.Append($"\"{name}\":{inputIdx}");
		}

	}
}
