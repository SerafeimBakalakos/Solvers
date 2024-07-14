namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using Newtonsoft.Json;

	public class InteropTempFile
	{
		protected readonly string tempFilePrefix;

		public InteropTempFile(string workDirectory, string fileNameAndExtension)
		{
			var time = DateTime.Now;
			tempFilePrefix = $"{workDirectory}\\{time.Year}-{time.Month}-{time.Day}-{time.Hour}{time.Minute}_{Guid.NewGuid()}";
			Path = tempFilePrefix + fileNameAndExtension;
		}

		[JsonIgnore]
		public string Path { get; }

		public void WriteToFileSystem()
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
