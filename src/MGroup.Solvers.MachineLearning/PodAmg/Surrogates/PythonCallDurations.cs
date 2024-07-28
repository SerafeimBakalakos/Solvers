namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// All timings are in milliseconds.
	/// </summary>
	public class PythonCallDurations
	{
		public long DataArraysPreparation { get; set; }

		public long IO { get; set; }

		public long PythonActualWork { get; set; }

		public long SetupWork { get; set;}

		public void Include(long totalCallDuration, long pythonActualWork, long pythonSetupWork, long pythonIO) 
		{
			PythonActualWork += pythonActualWork;
			SetupWork += totalCallDuration - pythonActualWork - pythonIO;
			IO += pythonIO;
		}

		public string Report()
		{
			var msg = new StringBuilder();
			msg.Append($"Total duration = {PythonActualWork + SetupWork + IO + DataArraysPreparation} ms.");
			msg.Append($" Data arrays preparation (in C#) = {DataArraysPreparation} ms.");
			msg.Append($" Actual work (in Python) = {PythonActualWork} ms.");
			msg.Append($" Setup work = {SetupWork} ms.");
			msg.Append($" IO operations = {IO} ms.");
			return msg.ToString();
		}
	}
}
