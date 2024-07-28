namespace MGroup.Solvers.MachineLearning.StochasticExtensions.Statistics
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IDistribution
	{
		double GenerateSample();

		double[] GenerateSamples(int count)
		{
			var result = new double[count];
			for (var i = 0; i < count; i++)
			{
				result[i] = GenerateSample();
			}

			return result;
		}

		double[,] GenerateSamples(int countAlongDim0, int countAlongDim1)
		{
			var result = new double[countAlongDim0, countAlongDim1];
			for (var i = 0; i < countAlongDim0; i++)
			{
				for (var j = 0; j < countAlongDim1; j++)
				{
					result[i, j] = GenerateSample();
				}
			}

			return result;
		}

		double[,,] GenerateSamples(int countAlongDim0, int countAlongDim1, int countAlongDim2)
		{
			var result = new double[countAlongDim0, countAlongDim1, countAlongDim2];
			for (var i = 0; i < countAlongDim0; i++)
			{
				for (var j = 0; j < countAlongDim1; j++)
				{
					for (var k = 0; k < countAlongDim2; k++)
					{
						result[i, j, k] = GenerateSample();
					}
				}
			}

			return result;
		}
	}
}
