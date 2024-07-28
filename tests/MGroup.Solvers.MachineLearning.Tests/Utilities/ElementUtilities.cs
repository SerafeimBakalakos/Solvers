namespace MGroup.Solvers.MachineLearning.Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;

	public static class ElementUtilities
	{
		public static double[] FindElementCentroid(CellType cellType, IReadOnlyList<INode> elementNodes)
		{
			if (cellType == CellType.Line2)
			{
				double x0 = 0;
				for (int i = 0; i < elementNodes.Count; i++)
				{
					x0 += elementNodes[i].X;
				}

				x0 /= elementNodes.Count;
				return new double[] { x0 };
			}
			else if ((cellType == CellType.Tri3) || (cellType == CellType.Quad4))
			{
				var x0 = new double[2];
				for (int i = 0; i < elementNodes.Count; i++)
				{
					x0[0] += elementNodes[i].X;
					x0[1] += elementNodes[i].Y;
				}

				x0[0] /= elementNodes.Count;
				x0[1] /= elementNodes.Count;
				return x0;
			}
			else if ((cellType == CellType.Tet4) || (cellType == CellType.Hexa8))
			{
				var x0 = new double[3];
				for (int i = 0; i < elementNodes.Count; i++)
				{
					x0[0] += elementNodes[i].X;
					x0[1] += elementNodes[i].Y;
					x0[2] += elementNodes[i].Z;
				}

				x0[0] /= elementNodes.Count;
				x0[1] /= elementNodes.Count;
				x0[2] /= elementNodes.Count;
				return x0;
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
