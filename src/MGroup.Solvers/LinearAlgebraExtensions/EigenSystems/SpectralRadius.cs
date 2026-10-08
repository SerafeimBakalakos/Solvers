namespace MGroup.Solvers.LinearAlgebraExtensions.EigenSystems
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using CSparse;

	using MGroup.LinearAlgebra.Implementations;
	using MGroup.LinearAlgebra.Implementations.Managed;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class SpectralRadius
	{
		private const double DoubleMachineEpsilon = 2.2204460492503131E-16;

		private readonly double tolerance = 0.01;
		private readonly int maxIterations = 15;
		private readonly int restart = 5;
		private readonly Random rng;
		private readonly ILapackProvider provider = DotNumericsLapackProvider.UniqueInstance;

		public SpectralRadius(double tolerance = 0.01, int maxIterations = 15, int restart = 5, int? rngSeed = null)
		{
			if (maxIterations < 1)
			{
				throw new ArgumentException("Expected maxIterations > 0.", nameof(maxIterations));
			}

			if (restart < 0)
			{
				throw new ArgumentException("Expected restart >= 0.", nameof(restart));
			}

			this.tolerance = tolerance;
			this.maxIterations = maxIterations;
			this.restart = restart;

			if (rngSeed is null) rng = new Random();
			else rng = new Random(rngSeed.Value);
		}

		public double ApproximateSpectralRadius(CsrMatrix matrix)
		{
			CheckSystemMatrix(matrix);

			int n = matrix.NumRows;
			Vector v0 = CreateRandomInitialGuess(n);
			Normalize(v0);

			double[] eigenvaluesReal = null;
			double[] eigenvaluesImaginary = null;

			for (int j = 0; j <= restart; ++j)
			{
				(double[] eigenvectors, double[] real, double[] imaginary, double[,] h, List<Vector> v, bool breakdown) = ApproximateEigenvalues(matrix, v0);

				CheckEigenvaluesAreReal(imaginary);

				eigenvaluesReal = real;
				eigenvaluesImaginary = imaginary;

				int numVectors = real.Length;
				int maxIndex = FindLargestMagnitudeEigenvalue(real, imaginary);

				double error = h[numVectors, numVectors - 1] * eigenvectors[numVectors - 1 + maxIndex * numVectors];

				v0 = Vector.CreateZero(n);

				for (int column = 0; column < numVectors; ++column)
				{
					double coefficient = eigenvectors[column + maxIndex * numVectors];
					Vector vector = v[column];
					v0.AxpyIntoThis(vector, coefficient);
				}

				if (Math.Abs(error) / Math.Abs(real[maxIndex]) < tolerance)
				{
					break;
				}

				if (breakdown)
				{
					break;
				}
			}

			int dominantIndex = FindLargestMagnitudeEigenvalue(eigenvaluesReal, eigenvaluesImaginary);
			return Math.Sqrt(
				eigenvaluesReal[dominantIndex] * eigenvaluesReal[dominantIndex] +
				eigenvaluesImaginary[dominantIndex] * eigenvaluesImaginary[dominantIndex]);
		}

		private (double[] Eigenvectors, double[] EigenvaluesReal, double[] EigenvaluesImaginary, double[,] H, List<Vector> V, bool Breakdown)
			ApproximateEigenvalues(CsrMatrix matrix, Vector initialGuess)
		{
			int n = matrix.NumRows;
			int iterationCount = Math.Min(n, maxIterations);
			double breakdownTolerance = GetBreakdownTolerance();

			double[,] h = new double[iterationCount + 1, iterationCount];
			var v = new List<Vector>(iterationCount + 1);

			v.Add(initialGuess);

			bool breakdown = false;
			int j = 0;

			for (; j < iterationCount; ++j)
			{
				Vector w = matrix.Multiply(v[v.Count - 1]);

				for (int i = 0; i < v.Count; ++i)
				{
					double hij = v[i].DotProduct(w);
					h[i, j] = hij;
					w.AxpyIntoThis(v[i], -hij);
				}

				double beta = w.Norm2();
				h[j + 1, j] = beta;

				if (beta < breakdownTolerance)
				{
					breakdown = true;

					if (beta != 0.0)
					{
						w.ScaleIntoThis(1.0 / beta);
					}

					v.Add(w);
					break;
				}

				w.ScaleIntoThis(1.0 / beta);
				v.Add(w);
			}

			int hSize = j + 1;

			double[] eigenvaluesReal = new double[hSize];
			double[] eigenvaluesImaginary = new double[hSize];
			double[] eigenvectors = ComputeEigenpairs(h, hSize, eigenvaluesReal, eigenvaluesImaginary);

			return (eigenvectors, eigenvaluesReal, eigenvaluesImaginary, h, v, breakdown);
		}

		private double[] ComputeEigenpairs(double[,] h, int size, double[] eigenvaluesReal, double[] eigenvaluesImaginary)
		{
			// Dgeev overwrites its input, so copy the square leading portion
			// of H into a column-major array.
			double[] a = new double[size * size];

			for (int column = 0; column < size; ++column)
			{
				for (int row = 0; row < size; ++row)
				{
					a[row + column * size] = h[row, column];
				}
			}

			double[] vl = new double[1];
			double[] vr = new double[size * size];
			double[] work = new double[1];
			int info = 0;

			// Query the optimal workspace size.
			provider.Dgeev(
				"N",
				"V",
				size,
				ref a,
				0,
				size,
				ref eigenvaluesReal,
				0,
				ref eigenvaluesImaginary,
				0,
				ref vl,
				0,
				1,
				ref vr,
				0,
				size,
				ref work,
				0,
				-1,
				ref info);

			if (info != 0)
			{
				throw new InvalidOperationException($"Dgeev workspace query failed with INFO = {info}.");
			}

			int workSize = Math.Max(1, (int)Math.Ceiling(work[0]));
			work = new double[workSize];

			provider.Dgeev(
				"N",
				"V",
				size,
				ref a,
				0,
				size,
				ref eigenvaluesReal,
				0,
				ref eigenvaluesImaginary,
				0,
				ref vl,
				0,
				1,
				ref vr,
				0,
				size,
				ref work,
				0,
				workSize,
				ref info);

			if (info != 0)
			{
				throw new InvalidOperationException($"Dgeev failed with INFO = {info}.");
			}

			return vr;
		}

		private void CheckEigenvaluesAreReal(double[] eigenvaluesImaginary)
		{
			for (int i = 0; i < eigenvaluesImaginary.Length; ++i)
			{
				if (eigenvaluesImaginary[i] != 0.0)
				{
					throw new InvalidOperationException(
						"Approximate spectral radius encountered a complex Ritz eigenvalue.");
				}
			}
		}

		private void CheckSystemMatrix(CsrMatrix matrix)
		{
			if (matrix.NumRows != matrix.NumColumns)
			{
				throw new ArgumentException("Expected a square matrix.", nameof(matrix));
			}

			if (matrix.NumRows == 0)
			{
				throw new ArgumentException("The matrix must not be empty.", nameof(matrix));
			}
		}

		private double GetBreakdownTolerance()
		{
			// Similar to PyAMG's set_tol(): 1e6 * eps for double precision.
			return 1.0E6 * DoubleMachineEpsilon;
		}

		private Vector CreateRandomInitialGuess(int length)
		{
			var vector = new double[length];
			for (int i = 0; i < length; ++i)
			{
				vector[i] = rng.NextDouble();
			}
			return Vector.CreateFromArray(vector);
		}

		private static void Normalize(Vector vector)
		{
			double norm = vector.Norm2();
			vector.ScaleIntoThis(1.0 / norm);
		}

		private static int FindLargestMagnitudeEigenvalue(double[] real, double[] imaginary)
		{
			int maxIndex = 0;
			double maxMagnitudeSquared = real[0] * real[0] + imaginary[0] * imaginary[0];

			for (int i = 1; i < real.Length; ++i)
			{
				double magnitudeSquared = real[i] * real[i] + imaginary[i] * imaginary[i];

				if (magnitudeSquared > maxMagnitudeSquared)
				{
					maxMagnitudeSquared = magnitudeSquared;
					maxIndex = i;
				}
			}

			return maxIndex;
		}
	}
}
