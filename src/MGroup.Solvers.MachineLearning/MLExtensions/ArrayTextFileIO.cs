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

		public void WriteArray1DToFile(float[] array, string file)
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

		public void WriteArray2DToFile(double[,] array, string file)
		{
			int m = array.GetLength(0);
			int n = array.GetLength(1);
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					for (int i = 0; i < m; i++)
					{
						writer.Write(array[i, 0]);
						for (int j = 1; j < n; j++)
						{
							writer.Write(separator);
							writer.Write(array[i, j].ToString("G"));
						}

						if (i < m-1)
						{
							writer.WriteLine();
						}
					}
				}
			}
		}

		public void WriteArray2DToFile(float[,] array, string file)
		{
			int m = array.GetLength(0);
			int n = array.GetLength(1);
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					for (int i = 0; i < m; i++)
					{
						writer.Write(array[i, 0]);
						for (int j = 1; j < n; j++)
						{
							writer.Write(separator);
							writer.Write(array[i, j].ToString("G"));
						}

						if (i < m - 1)
						{
							writer.WriteLine();
						}
					}
				}
			}
		}
	}
}
