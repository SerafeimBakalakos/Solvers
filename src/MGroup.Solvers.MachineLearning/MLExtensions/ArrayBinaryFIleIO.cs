namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using NumSharp;

	public class ArrayBinaryFileIO : IArrayFileIO
	{
		public void ReadArray1DFromFile(double[] array, string file)
		{
			double[] arr = np.Load<double[]>(file);
			Array.Copy(arr, array, array.Length);
		}

		public void ReadArray1DFromFile(float[] array, string file)
		{
			float[] arr = np.Load<float[]>(file);
			Array.Copy(arr, array, array.Length);
		}

		public float[,] ReadArray2DFromFile(string file)
		{
			return np.Load<float[,]>(file);
		}

		public void WriteArray1DToFile(double[] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray1DToFile(float[] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray2DToFile(double[,] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray2DToFile(float[,] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray3DToFile(float[,,] array, string file)
		{
			np.Save(array, file);
		}
	}
}
