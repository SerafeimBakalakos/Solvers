namespace MGroup.Solvers.LinearAlgebraExtensions.Random
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class SystemRandomRng : IRandomNumberGenerator
	{
		private readonly Random rng;

		public SystemRandomRng(int? seed = null)
		{
			if (seed is int castedSeed)
			{
				this.rng = new Random(castedSeed);
			}
			else
			{
				this.rng = new Random();
			}
		}

		public double NextDouble() => rng.NextDouble();

		public void NextDoubleArray(double[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = rng.NextDouble();
			}
		}

		public int NextInt32(int min, int max) => rng.Next(min, max);

		public int NextInt32() => rng.Next();

		public void NextInt32Array(int[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = rng.Next();
			}
		}

		public void NextInt32Array(int min, int max, int[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = rng.Next(min, max);
			}
		}
	}
}
