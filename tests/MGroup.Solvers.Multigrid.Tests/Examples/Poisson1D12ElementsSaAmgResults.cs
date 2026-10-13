using MGroup.LinearAlgebra.Matrices;

using Newtonsoft.Json.Linq;

namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.LinearAlgebra.Matrices;
	using MGroup.Solvers.Multigrid.Algebraic.SmoothAggregation.Aggregates;
	using MGroup.Solvers.Multigrid.Tests.Utilities;

	public static class Poisson1D12ElementsSaAmgResults
	{
		public static int[] SystemOrders = { 11, 4 };

		public static (int[] aggregatesOfFineDofs, int[] fineDofRootsOfAggregates, int[][] fineDofsPerAggregate) GetAggregates(int level)
		{
			if (level == 0)
			{
				int[] aggregatesOfFineDofs = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
				int[] fineDofRootsOfAggregates = [0, 3, 6, 9];
				int[][] fineDofsPerAggregate = new int[][]
				{
					new int[] { 0, 1 },
					new int[] { 2, 3, 4 },
					new int[] { 5, 6, 7 },
					new int[] { 8, 9, 10 }
				};
				return (aggregatesOfFineDofs, fineDofRootsOfAggregates, fineDofsPerAggregate);
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// The candidate vectors for the last level will not be relaxed/improved, since the algorithm stops immediately before that. But they will be influenced by the relaxation strategy of previous levels.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="relaxationStrategy"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public static double[,] GetNullspaceCandidates(int level, string relaxationStrategy = "SGS-4")
		{
			if (level == 0)
			{
				if (relaxationStrategy == "None")
				{
					return new double[,]
					{
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 },
						{ 1.0 }
					};
				}
				else if (relaxationStrategy == "SGS-4")
				{
					return new double[,]
					{
						{ 0.1824138483343726  },
						{ 0.3648276966687452  },
						{ 0.51654027872203312 },
						{ 0.62889222031984626 },
						{ 0.69871603322339526 },
						{ 0.72553998066967385 },
						{ 0.70924798315694326 },
						{ 0.64904699130988908 },
						{ 0.54400501612116925 },
						{ 0.39517555515785957 },
						{ 0.20892285858466839 }
					};
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else if (level  == 1)
			{
				if (relaxationStrategy == "None")
				{
					return new double[,]
					{
						{ 1.4142135623730951 },
						{ 1.7320508075688772 },
						{ 1.7320508075688772 },
						{ 1.7320508075688772 }
					};
				}
				else if (relaxationStrategy == "SGS-4")
				{
					return new double[,]
					{
						{ 0.40788976491299395 },
						{ 1.0726245286233662  },
						{ 1.2044513116314672  },
						{ 0.70409796037093131 }
					};
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		public static CsrMatrix GetProlongationMatrix(int level, double weightFactor = 4.0 / 3.0, int degree = 1)
		{
			if (level == 0)
			{
				if (Math.Abs(weightFactor - 4.0 / 3.0) < 1E-12 && (degree == 1))
				{
					double[] values = [0.44721359549995787, 0.43946231946747416, 0.16330447332925882, 0.30330991435496107, 0.35378237297489812, 0.57286632596987519, 0.4084335800396765, 0.20427429161577376, 0.22089943129809281, 0.39352088824268538, 0.57649318203777056, 0.37308519897617232, 0.26200611704501675, 0.18273784754466887, 0.43894086379344932, 0.54322687121275459, 0.28580563115540425];
					int[] colIndices = [0, 0, 1, 0, 1, 1, 1, 2, 1, 2, 2, 2, 3, 2, 3, 3, 3];
					int[] rowOffsets = [0, 1, 3, 5, 6, 8, 10, 11, 13, 15, 16, 17];
					return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
				}
				else if (Math.Abs(weightFactor - 1.2) < 1E-12 && (degree == 3))
				{
					double[] values = [0.25029461106457573, 0.069096172368922, 0.3352264053752359, 0.19034780985786717, 0.2492749040616434, 0.3357416658606365, 0.01712475896002844, 0.11009004712085854, 0.4164203560136139, 0.08232169834486885, 0.02542713100327549, 0.36427618782365917, 0.2145200095133144, 0.22360852907689527, 0.3575116313960595, 0.0219645436778127, 0.08758681214223488, 0.41898190473178953, 0.10007151718617513, 0.0185184806441649, 0.3468417367034566, 0.24281028675493022, 0.20208285104515578, 0.37209494128251186, 0.07540750579467398, 0.38677780839988496, 0.01531931192772393, 0.24387999747655054];
					int[] colIndices = [0, 1, 0, 1, 0, 1, 2, 0, 1, 2, 0, 1, 2, 1, 2, 3, 1, 2, 3, 1, 2, 3, 2, 3, 2, 3, 2, 3];
					int[] rowOffsets = [0, 2, 4, 7, 10, 13, 16, 19, 22, 24, 26, 28];
					return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Assumes the prolongation was created with Jacobi smoothing (weight = 4/3, degree = 1)
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static CsrMatrix GetRestrictionMatrix(int level)
		{
			if (level == 0)
			{
				double[] values = [0.44721359549995787, 0.43946231946747416, 0.30330991435496107, 0.16330447332925882, 0.35378237297489812, 0.57286632596987519, 0.4084335800396765, 0.22089943129809281, 0.20427429161577376, 0.39352088824268538, 0.57649318203777056, 0.37308519897617232, 0.18273784754466887, 0.26200611704501675, 0.43894086379344932, 0.54322687121275459, 0.28580563115540425];
				int[] colIndices = [0, 1, 2, 1, 2, 3, 4, 5, 4, 5, 6, 7, 8, 7, 8, 9, 10];
				int[] rowOffsets = [0, 3, 8, 13, 17];
				return CsrMatrix.CreateFromArrays(4, 11, values, colIndices, rowOffsets, checkInput: true);
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		public static double[,] GetStrenghOfConnectionMatrix(int level, double theta)
		{
			if (level == 0)
			{
				if (theta == 0.0 || theta == 0.25 || theta == 0.5)
				{
					return new double[,]
					{
						{ 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5, 0.0 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0, 0.5 },
						{ 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 1.0 }
					};
				}
				else if (theta == 0.75 || theta == 1.0)
				{
					return MatrixUtilities.CreateIdentityAsArray(11);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}

		}

		public static CsrMatrix GetSystemMatrix(int level)
		{
			if (level == 0)
			{
				double[] values = [24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0, -12.0, -12.0, 24.0];
				int[] colIndices = [0, 1, 0, 1, 2, 1, 2, 3, 2, 3, 4, 3, 4, 5, 4, 5, 6, 5, 6, 7, 6, 7, 8, 7, 8, 9, 8, 9, 10, 9, 10];
				int[] rowOffsets = [0, 2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 31];
				return CsrMatrix.CreateFromArrays(11, 11, values, colIndices, rowOffsets, checkInput: true);
			}
			else if (level == 1)
			{
				// Assumes the prolongation was created with Jacobi smoothing (weight = 4/3, degree = 1)
				double[] values = [3.727133566128809, -1.1238021267252114, -1.1238021267252118, 2.6634204401863526, -1.3139766928396734, -1.3139766928396732, 2.6642539625047736, -1.2723623611503245, -1.2723623611503245, 3.1053508194264161];
				int[] colIndices = [0, 1, 0, 1, 2, 1, 2, 3, 2, 3];
				int[] rowOffsets = [0, 2, 5, 8, 10];
				return CsrMatrix.CreateFromArrays(4, 4, values, colIndices, rowOffsets, checkInput: true);
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		public static CsrMatrix GetTentativeProlongation(int level, string relaxationStrategy = "SGS-4")
		{
			if (level == 0)
			{
				if (relaxationStrategy == "None")
				{
					double[] values = [0.7071067811865475, 0.7071067811865475, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258, 0.5773502691896258];
					int[] colIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
					int[] rowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
					return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
				}
				else if (relaxationStrategy == "SGS-4")
				{
					double[] values = [0.44721359549995787, 0.89442719099991574, 0.48156672249978671, 0.58631161560978162, 0.65140784550223307, 0.60238215830153163, 0.58885566922273058, 0.53887358089280879, 0.77262688821677283, 0.56125081650524045, 0.29672413548052906];
					int[] colIndices = [0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3];
					int[] rowOffsets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
					return CsrMatrix.CreateFromArrays(11, 4, values, colIndices, rowOffsets, checkInput: true);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}

//double[] values = [];
//int[] colIndices = [];
//int[] rowOffsets = [];
//return CsrMatrix.CreateFromArrays(m, n, values, colIndices, rowOffsets, checkInput: true);
