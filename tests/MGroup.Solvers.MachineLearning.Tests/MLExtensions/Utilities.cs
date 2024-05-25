namespace MGroup.Solvers.MachineLearning.Tests.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	internal static class Utilities
	{
		public static float[] ToFloat32(this double[] array)
		{
			var result = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				result[i] = (float)array[i];
			}

			return result;
		}

		public static float[,] ToFloat32(this double[,] array)
		{
			var result = new float[array.GetLength(0), array.GetLength(1)];
			for (int i = 0; i < array.GetLength(0); i++)
			{
				for (int j = 0; j < array.GetLength(1); j++)
				{
					result[i,j] = (float)array[i,j];
				}
			}

			return result;
		}
	}
}
