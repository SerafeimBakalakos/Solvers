namespace MGroup.Solvers.MachineLearning.MLExtensions.PythonInterop
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public interface IArrayFileIO
	{
		float[] ReadArray1DFloat32(string file, int length = -1);

		double[] ReadArray1DFloat64(string file, int length = -1);

		float[,] ReadArray2DFloat32(string file, (int dim0, int dim1)? shape = null);

		double[,] ReadArray2DFloat64(string file, (int dim0, int dim1)? shape = null);

		float[,,] ReadArray3DFloat32(string file, (int dim0, int dim1, int dim3)? shape = null);

		double[,,] ReadArray3DFloat64(string file, (int dim0, int dim1, int dim3)? shape = null);

		//void ReadArray1DFromFile(double[] array, string file);

		//void ReadArray1DFromFile(float[] array, string file);

		//float[,] ReadArray2DFromFile(string file);

		void WriteArray1DFloat32(float[] array, string file);

		void WriteArray1DFloat64(double[] array, string file);

		public void WriteArray2DFloat32(float[,] array, string file);

		public void WriteArray2DFloat64(double[,] array, string file);

		public void WriteArray3DFloat32(float[,,] array, string file);

		public void WriteArray3DFloat64(double[,,] array, string file);
	}
}
