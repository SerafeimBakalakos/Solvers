namespace MGroup.Solvers.MachineLearning.LinearAlgebraExtensions.PodAmg
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	using MGroup.LinearAlgebra.Eigensystems;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;

	public class ProperOrthogonalDecomposition
	{
		private readonly bool _keepOnlyNonZeroEigenvalues;
		private readonly double _zeroEigenvalueTolerance;

		public ProperOrthogonalDecomposition(bool keepOnlyNonZeroEigenvalues, double zeroEigenvalueTolerance = 1E-10)
		{
			_keepOnlyNonZeroEigenvalues = keepOnlyNonZeroEigenvalues;
			_zeroEigenvalueTolerance = zeroEigenvalueTolerance;
		}

		/// <summary>
		/// Performs POD analysis and returns the principal components of a samples set
		/// </summary>
		/// <param name="numSampleVectors">
		/// The number of sample vectors. Must be equal to the number of columns in <paramref name="sampleVectors"/> and &gt;= 2.
		/// </param>
		/// <param name="sampleVectors">
		/// A (d x n) matrix, which contains n vectors of length d. Each of these n vectors corresponds to one sample, time-step, 
		/// etc. In general n &lt; d, which is used here for increased performance.
		/// </param>
		/// <param name="numPrincipalComponents">
		/// How many principal components to keep. Depending on the configuration of this object, if some eigenvectors 
		/// correspond to zero eigenvalues, they will be discarded and fewer total eigenvectors will be returned.
		/// </param>
		/// <returns></returns>
		public Matrix CalculatePrincipalComponents(int numSampleVectors, Matrix sampleVectors, int numPrincipalComponents)
		{
			//CheckDependentEigenvectors(sampleVectors, "sample solutions");

			if (sampleVectors.NumColumns != numSampleVectors)
			{
				throw new ArgumentException("The matrix containing the sample vectors must have " +
					$"{numSampleVectors} columns but was ({sampleVectors.NumRows}, {sampleVectors.NumColumns}).");
			}
			if (numSampleVectors < 2)
			{
				throw new ArgumentException("There must be at least 2 vectors (columns) in the input matrix.");
			}
			if (numPrincipalComponents > sampleVectors.NumColumns)
			{
				throw new ArgumentException(
					"Cannot request more principal components than the number of vectors (columns) in the input matrix.");
			}

			// If d > n (usual case): Psi = eigenvectors of U^T*U and Phi = U * Psi
			// If d <= n: Phi = eigenvectors of U*U^T.
			if (sampleVectors.NumRows > sampleVectors.NumColumns)
			{
				Matrix correlation = sampleVectors.MultiplyRight(sampleVectors, transposeThis: true, transposeOther: false);
				(Vector eigenValues, Matrix eigenVectors) = PerformEigenDecomposition(correlation, true);

				int numComponentsToKeep = CountPrincipalComponentsToKeep(numPrincipalComponents, eigenValues);
				Matrix importantEigenVectors = eigenVectors.GetSubmatrix(0, eigenVectors.NumRows, 0, numComponentsToKeep); //TODO: discard the unneeded vectors earlier.
				Matrix principalComponents = sampleVectors * importantEigenVectors;
				return principalComponents;
			}
			else
			{
				//throw new NotImplementedException();
				Matrix correlation = sampleVectors.MultiplyRight(sampleVectors, transposeThis: false, transposeOther: true);
				(Vector eigenValues, Matrix eigenVectors) = PerformEigenDecomposition(correlation, true);

				int numComponentsToKeep = CountPrincipalComponentsToKeep(numPrincipalComponents, eigenValues);
				Matrix importantEigenVectors = eigenVectors.GetSubmatrix(0, eigenVectors.NumRows, 0, numComponentsToKeep); //TODO: discard the unneeded vectors earlier.
				Matrix principalComponents = importantEigenVectors;
				return principalComponents;
			}
		}

		private int CountPrincipalComponentsToKeep(int numComponentsRequested, Vector eigenvaluesDescending)
		{
			#region debug
			// να εκτυπωνω ποσα κραταω τελικα, και ποια απο αυτα ειναι γραμμικως εξαρτημενα
			//var msg = new StringBuilder();
			//msg.Append($"Num eigenvectors total = {eigenvaluesDescending.Length}. ");
			//msg.Append($"Num eigenvectors requested = {numComponentsRequested}. ");
			#endregion
			if (_keepOnlyNonZeroEigenvalues)
			{
				var numComponentsToKeep = 0;
				for (var i = 0; i < numComponentsRequested; ++i)
				{
					if (Math.Abs(eigenvaluesDescending[i]) <= _zeroEigenvalueTolerance) // Only keep eigenvectors of non-zero eigenvalues
					{
						#region debug
						//msg.Append($"Num eigenvalues above zero tolerance = {i}. ");
						#endregion
						break;
					}
					++numComponentsToKeep;
				}
				//msg.Append($"Num eigenvectors kept finally = {numComponentsToKeep}. ");
				//Console.WriteLine(msg);
				return numComponentsToKeep;
			}
			else
			{
				//msg.Append($"Num eigenvectors kept finally = {numComponentsRequested}. ");
				//Console.WriteLine(msg);
				return numComponentsRequested;
			}
		}

		private (Vector eigenValues, Matrix eigenVectors) PerformEigenDecomposition(Matrix matrix, bool useSvdAlgorithm)
		{
			if (useSvdAlgorithm)
			{
				var svd = SingularValueDecomposition.Calculate(matrix);
				//CheckDependentEigenvectors(svd.SingularVectors, "eigenvectors");
				return (svd.SingularValues, svd.SingularVectors);
			}
			else
			{
				//TODO: Make sure the eigenvalues are in descending order (and eigenvectors match them)
				var eigenDecomp = SymmetricEigensystemFull.Create(matrix.NumColumns, matrix.RawData, true);
				//CheckDependentEigenvectors(eigenDecomp.EigenvectorsRight, "eigenvectors");
				return (eigenDecomp.EigenvaluesReal, eigenDecomp.EigenvectorsRight);
			}
		}

		#region debug
		private void CheckDependentEigenvectors(Matrix samples, string columnVectorsDescription)
		{
			(Matrix rref, List<int> independentCols) = samples.ReducedRowEchelonForm();
			var writer = new LinearAlgebra.Output.FullMatrixWriter();
			writer.ArrayFormat = new LinearAlgebra.Output.Formatting.Array2DFormat("\n[", "]\n", "[ ", " ]" + Environment.NewLine, ", ");
			string path = "C:\\Users\\Serafeim\\Desktop\\AISolve\\CantileverDynamicLinear\\printed_matrices\\rhs_vectors.txt";
			writer.WriteToFile(samples, path);
			Console.WriteLine($"Independent {columnVectorsDescription}: {independentCols.Count}");
		}
		#endregion
	}
}
