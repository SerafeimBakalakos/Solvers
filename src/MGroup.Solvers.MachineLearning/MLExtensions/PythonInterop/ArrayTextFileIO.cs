namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	public class ArrayTextFileIO : IArrayFileIO
	{
		private readonly char separator1D;
		private readonly string separator2D;


		public ArrayTextFileIO(char separator1D = ' ', string separator2D = null)
		{
			this.separator1D = separator1D;
			this.separator2D = separator2D == null ? Environment.NewLine : separator2D;
		}

		public float[] ReadArray1DFloat32(string file, int length = -1)
		{
			using (var reader = new StreamReader(file))
			{
				var allWords = reader.ReadLine().Split(separator1D, StringSplitOptions.RemoveEmptyEntries);
				if (length > 0) CheckDimensions(length, allWords.Length);

				var result = new float[allWords.Length];
				for (var i = 0; i < allWords.Length; i++)
				{
					result[i] = float.Parse(allWords[i]);
				}
				return result;
			}
		}

		public double[] ReadArray1DFloat64(string file, int length = -1)
		{
			using (var reader = new StreamReader(file))
			{
				var allWords = reader.ReadLine().Split(separator1D, StringSplitOptions.RemoveEmptyEntries);
				if (length > 0) CheckDimensions(length, allWords.Length);

				var result = new double[allWords.Length];
				for (var i = 0; i < allWords.Length; i++)
				{
					result[i] = double.Parse(allWords[i]);
				}
				return result;
			}
		}

		public float[,] ReadArray2DFloat32(string file, (int dim0, int dim1)? shape = null)
			=> throw new NotImplementedException();

		public double[,] ReadArray2DFloat64(string file, (int dim0, int dim1)? shape = null)
			=> throw new NotImplementedException();

		public float[,,] ReadArray3DFloat32(string file, (int dim0, int dim1, int dim3)? shape = null)
			=> throw new NotImplementedException();

		public double[,,] ReadArray3DFloat64(string file, (int dim0, int dim1, int dim3)? shape = null)
			=> throw new NotImplementedException();

		public void WriteArray1DFloat32(float[] array, string file)
		{
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					writer.Write(array[0]);
					for (var i = 1; i < array.Length; i++)
					{
						writer.Write(separator1D);
						writer.Write(array[i].ToString("G"));
					}
				}
			}
		}

		public void WriteArray1DFloat64(double[] array, string file)
		{
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					writer.Write(array[0]);
					for (var i = 1; i < array.Length; i++)
					{
						writer.Write(separator1D);
						writer.Write(array[i].ToString("G"));
					}
				}
			}
		}

		public void WriteArray2DFloat64(double[,] array, string file)
		{
			var m = array.GetLength(0);
			var n = array.GetLength(1);
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					for (var i = 0; i < m; i++)
					{
						writer.Write(array[i, 0]);
						for (var j = 1; j < n; j++)
						{
							writer.Write(separator1D);
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

		public void WriteArray2DFloat32(float[,] array, string file)
		{
			var m = array.GetLength(0);
			var n = array.GetLength(1);
			using (var f = File.Open(file, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(f))
				{
					for (var i = 0; i < m; i++)
					{
						writer.Write(array[i, 0]);
						for (var j = 1; j < n; j++)
						{
							writer.Write(separator1D);
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

		public void WriteArray3DFloat32(float[,,] array, string file) => throw new NotImplementedException();

		public void WriteArray3DFloat64(double[,,] array, string file) => throw new NotImplementedException();

		private static void CheckDimensions(int expected, int computed)
		{
			if (expected != computed)
			{
				throw new ArgumentException($"Invalid dimensions. Expected {expected}, but read {computed}");
			}
		}

		private static void CheckDimensions((int dim0, int dim1) expected, (int dim0, int dim1) computed)
		{
			if (expected.dim0 != computed.dim0 || expected.dim1 != computed.dim1)
			{
				throw new ArgumentException(
					$"Invalid dimensions. Expected ({expected.dim0} x {expected.dim1})," +
					$" but read ({computed.dim0} x {computed.dim1})");
			}
		}
	}
}
