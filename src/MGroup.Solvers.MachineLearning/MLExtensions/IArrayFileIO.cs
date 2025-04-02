namespace MGroup.Solvers.MachineLearning.MLExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IArrayFileIO
	{
		void ReadArray1DFromFile(double[] array, string file);

		void ReadArray1DFromFile(float[] array, string file);

		float[,] ReadArray2DFromFile(string file);

		void WriteArray1DToFile(double[] array, string file);

		void WriteArray1DToFile(float[] array, string file);

		public void WriteArray2DToFile(double[,] array, string file);

		public void WriteArray2DToFile(float[,] array, string file);

		public void WriteArray3DToFile(float[,,] array, string file);
	}
}
