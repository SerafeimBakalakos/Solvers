namespace MGroup.Solvers.LinearAlgebraExtensions.Random
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IRandomNumberGenerator
	{
		double NextDouble();

		double[] NextDoubleArray(int length)
		{
			var result = new double[length];
			NextDoubleArray(result);
			return result;
		}

		void NextDoubleArray(double[] array);

		int NextInt32();

		int NextInt32(int min, int max);

		int[] NextInt32Array(int length)
		{
			var result = new int[length];
			NextInt32Array(result);
			return result;
		}

		void NextInt32Array(int[] array);

		int[] NextInt32Array(int min, int max, int length)
		{
			var result = new int[length];
			NextInt32Array(min, max, result);
			return result;
		}

		void NextInt32Array(int min, int max, int[] array);
	}
}
