namespace MGroup.Solvers.MachineLearning.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public static class ArrayTypeUtilities
	{
		public static T[] Append<T>(T[] oldArray, T newValue)
		{
			var result = new T[oldArray.Length + 1];
			Array.Copy(oldArray, result, oldArray.Length);
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(double[] oldArray, int newValue)
		{
			var result = new double[oldArray.Length + 1];
			Array.Copy(oldArray, result, oldArray.Length);
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(int[] oldArray, double newValue)
		{
			var result = new double[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(int[] oldArray, int newValue)
		{
			var result = new double[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(double[] oldArray, float newValue)
		{
			var result = new double[oldArray.Length + 1];
			Array.Copy(oldArray, result, oldArray.Length);
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(float[] oldArray, double newValue)
		{
			var result = new double[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static double[] AppendAndConvertToDouble(float[] oldArray, float newValue)
		{
			var result = new double[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(float[] oldArray, int newValue)
		{
			var result = new float[oldArray.Length + 1];
			Array.Copy(oldArray, result, oldArray.Length);
			result[oldArray.Length] = newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(int[] oldArray, float newValue)
		{
			var result = new float[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(int[] oldArray, int newValue)
		{
			var result = new float[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(float[] oldArray, double newValue)
		{
			var result = new float[oldArray.Length + 1];
			Array.Copy(oldArray, result, oldArray.Length);
			result[oldArray.Length] = (float)newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(double[] oldArray, float newValue)
		{
			var result = new float[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = (float)(oldArray[i]);
			}
			result[oldArray.Length] = newValue;
			return result;
		}

		public static float[] AppendAndConvertToFloat(double[] oldArray, double newValue)
		{
			var result = new float[oldArray.Length + 1];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = (float)(oldArray[i]);
			}
			result[oldArray.Length] = (float)newValue;
			return result;
		}

		public static T[] Combine<T>(T[] firstArray, T[] secondArray)
		{
			var result = new T[firstArray.Length + secondArray.Length];
			Array.Copy(firstArray, result, firstArray.Length);
			Array.Copy(secondArray, 0, result, firstArray.Length, secondArray.Length);
			return result;
		}

		public static double[] ConvertToDouble(int[] oldArray)
		{
			var result = new double[oldArray.Length];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			return result;
		}

		public static double[] ConvertToDouble(float[] oldArray)
		{
			var result = new double[oldArray.Length];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			return result;
		}

		public static float[] ConvertToFloat(int[] oldArray)
		{
			var result = new float[oldArray.Length];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = oldArray[i];
			}
			return result;
		}

		public static float[] ConvertToFloat(double[] oldArray)
		{
			var result = new float[oldArray.Length];
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[i] = (float)(oldArray[i]);
			}
			return result;
		}

		public static T[] Prepend<T>(T newValue, T[] oldArray)
		{
			var result = new T[oldArray.Length + 1];
			result[0] = newValue;
			Array.Copy(oldArray, 0, result, 1, oldArray.Length);
			return result;
		}

		public static double[] PrependAndConvertToDouble(int newValue, double[] oldArray)
		{
			var result = new double[oldArray.Length + 1];
			result[0] = newValue;
			Array.Copy(oldArray, 0, result, 1, oldArray.Length);
			return result;
		}

		public static float[] PrependAndConvertToFloat(int newValue, double[] oldArray)
		{
			var result = new float[oldArray.Length + 1];
			result[0] = newValue;
			for (int i = 0; i < oldArray.Length; i++)
			{
				result[1 + i] = (float)(oldArray[i]);
			}
			return result;
		}
	}
}
