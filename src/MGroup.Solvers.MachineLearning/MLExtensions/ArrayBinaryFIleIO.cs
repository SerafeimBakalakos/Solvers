namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using NumSharp;

	public class ArrayBinaryFileIO : IArrayFileIO
	{
		public float[] ReadArray1DFloat32(string file, int length = -1)
		{
			float[] arr = np.Load<float[]>(file);
			if (length > 0) CheckDimensions(length, arr.Length);
			return arr;
		}

		public double[] ReadArray1DFloat64(string file, int length = -1)
		{
			double[] arr = np.Load<double[]>(file);
			if (length > 0) CheckDimensions(length, arr.Length);
			return arr;
		}

		public float[,] ReadArray2DFloat32(string file, (int dim0, int dim1)? shape = null)
		{
			float[,] arr = np.Load<float[,]>(file);
			if (shape != null)
			{
				CheckDimensions(shape.Value, (arr.GetLength(0), arr.GetLength(1)));
			}
			return arr;
		}

		public double[,] ReadArray2DFloat64(string file, (int dim0, int dim1)? shape = null)
		{
			double[,] arr = np.Load<double[,]>(file);
			if (shape != null)
			{
				CheckDimensions(shape.Value, (arr.GetLength(0), arr.GetLength(1)));
			}
			return arr;
		}

		public float[,,] ReadArray3DFloat32(string file, (int dim0, int dim1, int dim3)? shape = null)
		{
			float[,,] arr = np.Load<float[,,]>(file);
			if (shape != null)
			{
				CheckDimensions(shape.Value, (arr.GetLength(0), arr.GetLength(1), arr.GetLength(2)));
			}
			return arr;
		}

		public double[,,] ReadArray3DFloat64(string file, (int dim0, int dim1, int dim3)? shape = null)
		{
			double[,,] arr = np.Load<double[,,]>(file);
			if (shape != null)
			{
				CheckDimensions(shape.Value, (arr.GetLength(0), arr.GetLength(1), arr.GetLength(2)));
			}
			return arr;
		}

		public void WriteArray1DFloat32(float[] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray1DFloat64(double[] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray2DFloat32(float[,] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray2DFloat64(double[,] array, string file)
		{
			np.Save(array, file);
		}
		
		public void WriteArray3DFloat32(float[,,] array, string file)
		{
			np.Save(array, file);
		}

		public void WriteArray3DFloat64(double[,,] array, string file)
		{
			np.Save(array, file);
		}

		private static void CheckDimensions(int expected, int computed)
		{
			if (expected != computed)
			{
				throw new ArgumentException($"Invalid dimensions. Expected {expected}, but read {computed}");
			}
		}

		private static void CheckDimensions((int dim0, int dim1) expected, (int dim0, int dim1) computed)
		{
			if ((expected.dim0 != computed.dim0) || (expected.dim1 != computed.dim1))
			{
				throw new ArgumentException(
					$"Invalid dimensions. Expected ({expected.dim0} x {expected.dim1})," +
					$" but read ({computed.dim0} x {computed.dim1})");
			}
		}

		private static void CheckDimensions((int dim0, int dim1, int dim2) expected, (int dim0, int dim1, int dim2) computed)
		{
			if ((expected.dim0 != computed.dim0) || (expected.dim1 != computed.dim1) || (expected.dim2 != computed.dim2))
			{
				throw new ArgumentException(
					$"Invalid dimensions. Expected ({expected.dim0} x {expected.dim1} x {expected.dim2})," +
					$" but read ({computed.dim0} x {computed.dim1} x {computed.dim2})");
			}
		}
	}
}
