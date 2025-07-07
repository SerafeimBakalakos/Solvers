using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
    public class PythonCallBuilder
    {
		private readonly string workDirectory;
		private readonly string workDirectoryForPython;
		private readonly string pythonInterpreter;
		private readonly string pythonScript;

		private List<string> inputsSerializedOrdered = new List<string>();
		private HashSet<string> inputsSerializedNames = new HashSet<string>();
		private HashSet<string> inputsArraysNames = new HashSet<string>();
		private HashSet<string> outputsArraysNames = new HashSet<string>();
		private StringBuilder inputsSerializedTemplate = new StringBuilder();
		private StringBuilder inputsArraysTemplate = new StringBuilder();
		private StringBuilder outputsArraysTemplate = new StringBuilder();

		public PythonCallBuilder(string workDirectory, string pythonInterpreterPath, string pythonScriptPath)
		{
			this.workDirectory = workDirectory.TrimEnd('\\');
			this.workDirectoryForPython = workDirectory.Replace("\\", "/");
			this.pythonInterpreter = pythonInterpreterPath;
			this.pythonScript = pythonScriptPath;
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

		public PythonCall Build()
		{
			if (!Directory.Exists(workDirectory))
			{
				//TODO: Perhaps also do this immediately before writing to filesystem. See TOCTOU bugs.
				throw new DirectoryNotFoundException(workDirectory);
			}
			inputsSerializedTemplate.Append("}}");
			inputsArraysTemplate.Append("}}");
			outputsArraysTemplate.Append("}}");
			return new PythonCall(workDirectory, pythonInterpreter, pythonScript, CleanupIOFiles, TimeoutMilliseconds,
				inputsSerializedNames, inputsSerializedOrdered, inputsSerializedTemplate.ToString(), 
				inputsArraysNames, inputsArraysTemplate.ToString(), outputsArraysNames, outputsArraysTemplate.ToString());
		}

		public void DefineSmallInput(string name)
		{
			int inputIdx = inputsSerializedOrdered.Count;
			inputsSerializedNames.Add(name);
			inputsSerializedOrdered.Add(name);
			if (inputsSerializedTemplate.Length == 0)
			{
				inputsSerializedTemplate.Append("{{");
			}
			else
			{
				inputsSerializedTemplate.Append(',');
			}
			//inputsSerializedTemplate.Append('"');
			//inputsSerializedTemplate.Append(name);
			//inputsSerializedTemplate.Append('"');
			//inputsSerializedTemplate.Append(':');
			//inputsSerializedTemplate.Append(inputIdx);
			inputsSerializedTemplate.Append($"\"{name}\":{{{inputIdx}}}"); // placeholder
		}

		//TODO: Also specify array dimensions and type, but make them optional
		public void DefineArrayInput(string name)
		{
			inputsArraysNames.Add(name);
			if (inputsArraysTemplate.Length == 0)
			{
				inputsArraysTemplate.Append("{{");
			}
			else
			{
				inputsArraysTemplate.Append(',');
			}
			// Example: {...,"MyArray":"C:\\Users\\JohnDoe\\Desktop\\Project1\\{0}\\MyArray.npy",...}
			// where {0} will be replaced (when the call is executed) with time_guid, e.g. 2025-11-29-1221_bbab4f04-bf62-4026-af85-c97ede0adb6f
			string path = workDirectoryForPython + "/{0}/" + name + ".npy";
			inputsArraysTemplate.Append($"\"{name}\":\"{path}\"");
		}

		public void DefineArrayOutput(string name)
		{
			outputsArraysNames.Add(name);
			if (outputsArraysTemplate.Length == 0)
			{
				outputsArraysTemplate.Append("{{");
			}
			else
			{
				outputsArraysTemplate.Append(',');
			}
			// Example: {...,"MyArray":"C:\\Users\\JohnDoe\\Desktop\\Project1\\{0}\\MyArray.npy",...}
			// where {0} will be replaced (when the call is executed) with time_guid, e.g. 2025-11-29-1221_bbab4f04-bf62-4026-af85-c97ede0adb6f
			string path = workDirectoryForPython + "/{0}/" + name + ".npy";
			outputsArraysTemplate.Append($"\"{name}\":\"{path}\"");
		}
	}
}
