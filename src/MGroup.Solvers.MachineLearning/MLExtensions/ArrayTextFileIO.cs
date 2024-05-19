namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	public class ArrayTextFileIO
	{
		private readonly char separator;

		public ArrayTextFileIO(char separator = ' ')
		{
			this.separator = separator;
		}

		public void ReadArray1DFromFile(double[] array, string file)
		{
			using (var reader = new StreamReader(file))
			{
				string[] allWords = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = double.Parse(allWords[i]);
				}
			}
		}

		public void ReadArray1DFromFile(float[] array, string file)
		{
			using (var reader = new StreamReader(file))
			{
				string[] allWords = reader.ReadLine().Split(separator, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = float.Parse(allWords[i]);
				}
			}
		}

		public void WriteArray1DToFile(double[] array, string file)
		{
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					writer.Write(array[0]);
					for (int i = 1; i < array.Length; i++)
					{
						writer.Write(separator);
						writer.Write(array[i].ToString("G"));
					}
				}
			}
		}
	}
}
