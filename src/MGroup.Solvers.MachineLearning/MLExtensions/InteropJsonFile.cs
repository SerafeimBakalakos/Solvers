namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using Newtonsoft.Json;

	internal class InteropJsonFile
	{
		public InteropJsonFile(string path)
		{
			Path = path;
		}

		[JsonIgnore]
		public string Path { get; }

		internal void WriteToFileSystem()
		{
			using (StreamWriter file = File.CreateText(Path))
			{
				var serializer = new JsonSerializer();
				serializer.Serialize(file, this);
			}

			// For .NET Core 3.0+ and .NET 5+, instead of Newtonsoft lib:
			//string json = JsonSerializer.Serialize(settings);
			//File.WriteAllText(path, json);
		}
	}
}
