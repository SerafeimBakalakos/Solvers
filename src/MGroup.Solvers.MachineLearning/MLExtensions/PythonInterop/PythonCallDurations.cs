//TODO: use this to time, instead of managing several stopwatches in PythonCall
namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// All timings are in milliseconds.
	/// </summary>
	public class PythonCallDurations 
	{
		/// <summary>
		/// Milliseconds spent by C# and Python code on: converting and transfering data, running system processes, etc.
		/// Both C# and Python scripts need to time their respective operations.
		/// </summary>
		public long CommunicationWork { get; set; }

		/// <summary>
		/// Milliseconds spent on the actual work that the Python script needs to do. E.g. using the preloaded inputs and 
		/// trained models to predict a new output. The Python script does not specify this, but C# infers it. Anything not timed
		/// and labeled as "CommunicationWork" or "SetupWork" by the Python code, will be counted as actual work.
		/// </summary>
		public long PythonActualWork { get; set; }

		/// <summary>
		/// Milliseconds spent on initializing data that would have been ready, if Python code was running continuously.
		/// E.g. loading trained models from filesystem. The Python script can specifies parts of it as "SetupWork".
		/// </summary>
		public long PythonSetupWork { get; set;}

		public void Include(long totalCallDuration, long pythonCommunicationWork, long pythonSetupWork) 
		{
			CommunicationWork += pythonCommunicationWork;
			PythonSetupWork += pythonSetupWork;
			PythonActualWork += totalCallDuration - pythonSetupWork;
		}

		public string Report()
		{
			var msg = new StringBuilder();
			msg.Append($"Total duration = {PythonActualWork + PythonSetupWork + CommunicationWork} ms.");
			msg.Append($" Actual work (in Python) = {PythonActualWork} ms.");
			msg.Append($" Setup work (in Python) = {PythonSetupWork} ms.");
			msg.Append($" C#-Python communication = {CommunicationWork} ms.");
			return msg.ToString();
		}
	}
}
