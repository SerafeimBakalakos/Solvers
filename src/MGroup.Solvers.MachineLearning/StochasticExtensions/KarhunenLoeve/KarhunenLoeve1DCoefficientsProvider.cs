using System;
using System.Collections.Generic;
using System.Linq;

using Accord.Math.Decompositions;

using MGroup.LinearAlgebra.Vectors;
using MGroup.Solvers.MachineLearning.StochasticExtensions.Statistics;

namespace MGroup.Solvers.MachineLearning.StochasticExtensions.KarhunenLoeve
{
	public class KarhunenLoeve1DCoefficientsProvider //: IUncertainParameterRealizer
	{
		private readonly IDistribution ksiNormalDistribution;
		private bool resetGeneration = true;
		private int previousIteration = -1;

		/// <summary>Initializes a new instance of the <see cref="KarhunenLoeve1DCoefficientsProvider"/> class.</summary>
		/// <param name="domainBounds">The minimum and maximum coordinates of the domain.</param>
		/// <param name="numNodes">The partition.</param>
		/// <param name="variableMean">The mean value of the random variable.</param>
		/// <param name="variableStdDev">The standard deviation of the random variable.</param>
		/// <param name="correlationLength">Length of the correlation.</param>
		/// <param name="numKarLoeveTerms">The kar loeve terms.</param>
		/// <param name="isGaussian">if set to <c>true</c> [is gaussian].</param>
		/// <param name="midpointMethod">if set to <c>true</c> [midpoint method].</param>
		public KarhunenLoeve1DCoefficientsProvider(double[] domainBounds, int numNodes, double variableMean, 
			double variableStdDev, double correlationLength, int numKarLoeveTerms, Random rng, 
			bool isGaussian = true, bool midpointMethod = true)
		{
			DomainBounds = domainBounds;
			NumNodes = numNodes;
			MeanValue = variableMean;
			SigmaSquare = variableStdDev * variableStdDev;
			CorrelationLength = correlationLength;
			NumKarLoeveTerms = numKarLoeveTerms;

			if (midpointMethod != true)
			{
				throw new NotImplementedException("Only midpointMethod == true is supported at the moment.");
			}
			MidpointMethod = midpointMethod;

			if (isGaussian != true)
			{
				throw new NotImplementedException("Only isGaussian == true is supported at the moment.");
			}
			IsGaussian = isGaussian;

			ksiNormalDistribution = NormalDistribution.CreateStandard(rng);
		}

		public double[] DomainBounds { get; }

		public int NumNodes { get; }

		public double MeanValue { get; }

		public double SigmaSquare { get; set; }

		public double CorrelationLength { get; set; }

		public int NumKarLoeveTerms { get; }

		public bool IsGaussian { get; }

		public bool MidpointMethod { get; }

		public double[] Xcoordinates { get; set; }

		public double[] Ksi { get; set; }

		public double[] Lambda { get; set; }

		public double[,] Eigenvectors { get; set; }

		public double[,] EigenModesAtPoint { get; set; }

		public double[] GenerateFieldAtNodes()
		{
			var fieldAtNodes = Vector.CreateWithValue(NumNodes, 1.0);
			for (var j = 0; j < NumKarLoeveTerms; j++)
			{
				var coeff = Math.Sqrt(Lambda[j]) * Ksi[j];
				for (var i = 0; i < NumNodes; i++)
				{
					fieldAtNodes[i] += coeff * Eigenvectors[i, j];
				}
			}

			fieldAtNodes.ScaleIntoThis(MeanValue);
			return fieldAtNodes.RawData;
		}

		public void Initialize()
		{
			(var xCoordinates, var lambda, var eigenvectors) =
				KarhunenLoeveFredholmWithFEM(NumKarLoeveTerms, DomainBounds, SigmaSquare, NumNodes, CorrelationLength);
			Xcoordinates = xCoordinates;
			Lambda = lambda;
			Eigenvectors = eigenvectors;
		}

		// Replaced by GenerateField()
		/// <summary>Realizes the specified iteration based on a stochastic domain mapper and the respective domain parameters.</summary>
		/// <param name="iteration">The iteration.</param>
		/// <param name="domainMapper">The domain mapper.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public double Realize(int iteration, IStochasticDomainMapper domainMapper, double[] parameters)
		{
			resetGeneration = previousIteration != iteration;
			if (resetGeneration) ResetSampleGeneration();
			var stochasticDomainPoint = domainMapper.Map(parameters);
			var eigenModesAtPoint = CalculateEigenmodesAtPoint(Xcoordinates, Eigenvectors, stochasticDomainPoint[0]);
			var value = KarhunenLoeveFredholm1DSampleGenerator(stochasticDomainPoint, Lambda, eigenModesAtPoint, MeanValue, MidpointMethod, IsGaussian);
			previousIteration = iteration;
			return value;
		}

		/// <summary>Resets the sample generation.</summary>
		public void ResetSampleGeneration()
		{
			Ksi = new double[NumKarLoeveTerms];
			for (var i = 0; i < NumKarLoeveTerms; i++)
			{
				Ksi[i] = ksiNormalDistribution.GenerateSample();
			}
		}

		/// <summary>Calculates the eigenmodes at specified point.</summary>
		/// <param name="xCoordinates">The x coordinates.</param>
		/// <param name="eigenModes">The eigen modes.</param>
		/// <param name="stochasticDomainPoint">The stochastic domain point.</param>
		/// <returns></returns>
		private double[] CalculateEigenmodesAtPoint(double[] xCoordinates, double[,] eigenModes, double stochasticDomainPoint)
		{
			var xCoordinatesList = xCoordinates.ToList();
			var firstXcoordinate = xCoordinatesList.OrderBy(item => Math.Abs(stochasticDomainPoint - item)).First();
			var indexOfFirstEigenmodeValue = xCoordinatesList.IndexOf(firstXcoordinate);
			var list = xCoordinatesList;
			list.Remove(firstXcoordinate);
			xCoordinatesList = xCoordinates.ToList();
			var secondXcoordinate = list.OrderBy(item => Math.Abs(stochasticDomainPoint - item)).First();
			var indexOfSecondEigenmodeValue = xCoordinatesList.IndexOf(secondXcoordinate);
			var eigenmodesAtPoint = new double[eigenModes.GetLength(1)];

			for (var j = 0; j < eigenModes.GetLength(1); j++)
			{
				eigenmodesAtPoint[j] = (eigenModes[indexOfSecondEigenmodeValue, j] - eigenModes[indexOfFirstEigenmodeValue, j]) /
									   (xCoordinates[indexOfSecondEigenmodeValue] - xCoordinates[indexOfFirstEigenmodeValue]) *
				(stochasticDomainPoint - xCoordinates[indexOfFirstEigenmodeValue]) + eigenModes[indexOfFirstEigenmodeValue, j];
			}
			return eigenmodesAtPoint;
		}

		/// <summary>Covariance function.</summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <param name="sigmaSquare">The sigma square.</param>
		/// <param name="correlationLength">Length of the correlation.</param>
		/// <returns></returns>
		private double GaussianKernelCovarianceFunction(double x, double y, double sigmaSquare, double correlationLength)
		{
			//CorrelationLength = correlationLength;
			//SigmaSquare = sigmaSquare;
			var nominator = -Math.Abs(x - y) / correlationLength;
			var correlationFunction = Math.Pow(Math.E, nominator) * sigmaSquare;
			return correlationFunction;
		}

		/// <summary>  Field realization based on KL expansion.</summary>
		/// <param name="stochasticDomainPoint">The stochastic domain point.</param>
		/// <param name="eigenValues">The eigen values.</param>
		/// <param name="eigenmodesAtPoint">The eigenmodes at point.</param>
		/// <param name="meanValue">The mean value.</param>
		/// <param name="midpointMethod">if set to <c>true</c> [midpoint method].</param>
		/// <param name="isGaussian">if set to <c>true</c> [is gaussian].</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">It is not supported at the moment
		/// or
		/// It is not supported at the moment</exception>
		private double KarhunenLoeveFredholm1DSampleGenerator(double[] stochasticDomainPoint, double[] eigenValues,
			double[] eigenmodesAtPoint, double meanValue, bool midpointMethod, bool isGaussian)
		{
			if (midpointMethod == false) throw new ArgumentException("It is not supported at the moment");
			if (isGaussian == false) throw new ArgumentException("It is not supported at the moment");
			double fieldRealization = 0;

			for (var j = 0; j < eigenmodesAtPoint.Length; j++)
			{
				fieldRealization = fieldRealization + Math.Sqrt(eigenValues[j]) * eigenmodesAtPoint[j] * Ksi[j];
			}

			if (fieldRealization >= .9)
			{
				fieldRealization = .9;
			}
			else if (fieldRealization <= -.9)
			{
				fieldRealization = -.9;
			}

			fieldRealization = meanValue * (1 + fieldRealization);
			return fieldRealization;
		}

		/// <summary>  Calculates the Fredholm integral that provides eigenvalues and eigenfunctions of the expansion.</summary>
		/// <param name="numKarLoeveTerms">The kar loeve terms.</param>
		/// <param name="domainBounds">The domain bounds.</param>
		/// <param name="sigmaSquare">The sigma square.</param>
		/// <param name="partition">The partition.</param>
		/// <param name="correlationLength">Length of the correlation.</param>
		/// <returns></returns>
		private (double[] xCoordinates, double[] lambda, double[,] Eigenvectors) KarhunenLoeveFredholmWithFEM(
			int numKarLoeveTerms, double[] domainBounds, double sigmaSquare, int partition, double correlationLength)
		{
			//FEM parameters 
			var ned = 1; //number of dof per node
			var nen = 2; //number of nodes per element
			var nnp = partition; //number of nodal points
			var nfe = nnp - 1; //number of finite elements
			var neq = ned * nen; //number of element equations
			var ndof = ned * nnp; // number of degrees of freedom
			var GaussLegendreOrder = 3;

			var xCoordinates = new double[partition];
			var IEN = new int[2, nfe];  //local to global
			var ID = new int[partition];

			for (var i = 0; i < partition; i++)
			{
				//LagrangianShapeFunctons();
				xCoordinates[i] = domainBounds[0] + (domainBounds[1] - domainBounds[0]) / nfe * i;
				ID[i] = i;
			}

			for (var i = 0; i < nfe; i++)
			{
				IEN[0, i] = i;
				IEN[1, i] = i + 1;
			}

			// localization matrix
			var LM = new int[partition - 1, 2];
			for (var i = 0; i < partition - 1; i++)
			{
				LM[i, 0] = i;
				LM[i, 1] = i + 1;
			}

			// computing B matrix
			var GaussLegendreCoordinates = gauss_quad().Item1;
			var GaussLegendreWeights = gauss_quad().Item2;
			var Bmatrix = new double[ndof, ndof];
			for (var i = 0; i < nfe; i++)
			{
				var Be = new double[neq, neq];
				var det_Je = (xCoordinates[IEN[1, i]] - xCoordinates[IEN[0, i]]) / 2;
				for (var j = 0; j < GaussLegendreOrder; j++)
				{
					var xi_gl = GaussLegendreCoordinates[j];
					var w_gl = GaussLegendreWeights[j];
					var NN = LagrangianShapeFunctions(xi_gl);
					//Be=Be+NN'*NN*det_Je*w_gl(j)
					Be[0, 0] = Be[0, 0] + NN[0] * NN[0] * det_Je * w_gl;
					Be[1, 0] = Be[1, 0] + NN[0] * NN[1] * det_Je * w_gl;
					Be[0, 1] = Be[0, 1] + NN[1] * NN[0] * det_Je * w_gl;
					Be[1, 1] = Be[1, 1] + NN[1] * NN[1] * det_Je * w_gl;
				}
				Bmatrix[LM[i, 0], LM[i, 0]] = Bmatrix[LM[i, 0], LM[i, 0]] + Be[0, 0];
				Bmatrix[LM[i, 1], LM[i, 0]] = Bmatrix[LM[i, 1], LM[i, 0]] + Be[1, 0];
				Bmatrix[LM[i, 0], LM[i, 1]] = Bmatrix[LM[i, 0], LM[i, 1]] + Be[0, 1];
				Bmatrix[LM[i, 1], LM[i, 1]] = Bmatrix[LM[i, 1], LM[i, 1]] + Be[1, 1];
			}

			// computing C matrix
			var Cmatrix = new double[ndof, ndof];
			for (var i = 0; i < nfe; i++)
			{
				double[] xe = { xCoordinates[IEN[0, i]], xCoordinates[IEN[1, i]] };
				var det_Je = (xCoordinates[IEN[1, i]] - xCoordinates[IEN[0, i]]) / 2;
				for (var j = 0; j < nfe; j++)
				{
					var Cef = new double[neq, neq];
					double[] xf = { xCoordinates[IEN[0, j]], xCoordinates[IEN[1, j]] };
					var det_Jf = (xCoordinates[IEN[1, j]] - xCoordinates[IEN[0, j]]) / 2;
					for (var k = 0; k < GaussLegendreOrder; k++)
					{
						var xi_gl_e = GaussLegendreCoordinates[k];
						var NNe = LagrangianShapeFunctions(xi_gl_e);
						var xpk = NNe[0] * xe[0] + NNe[1] * xe[1];
						for (var l = 0; l < GaussLegendreOrder; l++)
						{
							var xi_gl_f = GaussLegendreCoordinates[l];
							var NNf = LagrangianShapeFunctions(xi_gl_f);
							var xpl = NNf[0] * xf[0] + NNf[1] * xf[1];
							//element C matrix
							Cef[0, 0] = Cef[0, 0] + GaussianKernelCovarianceFunction(xpk, xpl, sigmaSquare, correlationLength) * NNe[0] * NNf[0] * det_Je * det_Jf * GaussLegendreWeights[k] * GaussLegendreWeights[l];
							Cef[1, 0] = Cef[1, 0] + GaussianKernelCovarianceFunction(xpk, xpl, sigmaSquare, correlationLength) * NNe[0] * NNf[1] * det_Je * det_Jf * GaussLegendreWeights[k] * GaussLegendreWeights[l];
							Cef[0, 1] = Cef[0, 1] + GaussianKernelCovarianceFunction(xpk, xpl, sigmaSquare, correlationLength) * NNe[1] * NNf[0] * det_Je * det_Jf * GaussLegendreWeights[k] * GaussLegendreWeights[l];
							Cef[1, 1] = Cef[1, 1] + GaussianKernelCovarianceFunction(xpk, xpl, sigmaSquare, correlationLength) * NNe[1] * NNf[1] * det_Je * det_Jf * GaussLegendreWeights[k] * GaussLegendreWeights[l];
						}
					}
					Cmatrix[LM[i, 0], LM[j, 0]] = Cmatrix[LM[i, 0], LM[j, 0]] + Cef[0, 0];
					Cmatrix[LM[i, 0], LM[j, 1]] = Cmatrix[LM[i, 0], LM[j, 1]] + Cef[0, 1];
					Cmatrix[LM[i, 1], LM[j, 0]] = Cmatrix[LM[i, 1], LM[j, 0]] + Cef[1, 0];
					Cmatrix[LM[i, 1], LM[j, 1]] = Cmatrix[LM[i, 1], LM[j, 1]] + Cef[1, 1];
				}

			}
			var sort = true;
			var gevd = new GeneralizedEigenvalueDecomposition(Cmatrix, Bmatrix, sort);
			var lambdaAll = gevd.RealEigenvalues;
			var lambda = lambdaAll.Skip(0).Take(numKarLoeveTerms).ToArray();
			var EigenvectorsAll = gevd.Eigenvectors;
			var Eigenvectors = new double[partition, numKarLoeveTerms];
			for (var i = 0; i < partition; i++)
			{
				for (var j = 0; j < numKarLoeveTerms; j++)
				{
					Eigenvectors[i, j] = EigenvectorsAll[i, j];  //each column corresponds to an eigenvector
				}
			}
			return (xCoordinates, lambda, Eigenvectors);
		}

		private static double[] LagrangianShapeFunctions(double xi)
		{
			double[] NShape = { -(xi - 1) / 2, (xi + 1) / 2 };
			return NShape;
		}

		private static Tuple<double[], double[]> gauss_quad()
		{
			double[] GaussLegendreCoordinates = { -0.7746, 0, 0.7746 };
			double[] GaussLegendreWeights = { 0.5556, 0.8889, 0.5556 };
			return new Tuple<double[], double[]>(GaussLegendreCoordinates, GaussLegendreWeights);
		}
	}
}
