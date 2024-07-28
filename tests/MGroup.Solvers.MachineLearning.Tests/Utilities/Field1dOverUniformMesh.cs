namespace MGroup.Solvers.MachineLearning.Tests.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.Solvers.MachineLearning.StochasticExtensions;

	public class Field1dOverUniformMesh
	{
		private readonly ICartesianMesh mesh;
		private readonly IRandomField1D randomField;
		private readonly int axisOfRandomField;
		private readonly Func<double, bool> isFieldValueValid;
		private Dictionary<int, double> centroidsOfElements;
		private Dictionary<int, double> fieldAtCentroids;

		public Field1dOverUniformMesh(ICartesianMesh mesh, IRandomField1D randomField, int axisOfRandomField,
			Func<double, bool> isFieldValueValid)
		{
			this.mesh = mesh;
			this.randomField = randomField;
			this.isFieldValueValid = isFieldValueValid;

			if ((axisOfRandomField != 0) && (axisOfRandomField != 1))
			{
				throw new ArgumentException(
					$"The 1D random field can be applied along axis 0 or 1, but {axisOfRandomField} was provided");
			}
			this.axisOfRandomField = axisOfRandomField;
		}

		public void GenerateValuesAtElementCentroids()
		{
			int dim = mesh.Dimension;
			centroidsOfElements = new Dictionary<int, double>();
			fieldAtCentroids = new Dictionary<int, double>();
			foreach ((int elementID, int[] nodeIDs) in mesh.EnumerateElements())
			{
				var centroidCoords = new double[dim];
				for (int n = 0; n < nodeIDs.Length; n++)
				{
					int[] nodeIdx = mesh.GetNodeIdx(nodeIDs[n]);
					double[] nodeCoords = mesh.GetNodeCoordinates(nodeIdx);
					for (int d = 0; d < dim; d++)
					{
						centroidCoords[d] += nodeCoords[d];
					}
				}

				for (int d = 0; d < dim; d++)
				{
					centroidCoords[d] /= nodeIDs.Length;
				}

				double centroidX = centroidCoords[axisOfRandomField];
				double fieldValue = randomField.CalcValueAt(centroidX);
				if (!isFieldValueValid(fieldValue))
				{
					throw new Exception($"The field at x={centroidX} evaluates to {fieldValue}, which is not valid");
				}

				centroidsOfElements[elementID] = centroidX;
				fieldAtCentroids[elementID] = fieldValue;
			}
		}

		public double GetValueOfElement(int elementID) => fieldAtCentroids[elementID];

		public void PrintFieldAtElements(int numElementsPerLine = 5)
		{
			int numElementsAtCurrentLine = 0;
			var msg = new StringBuilder();
			msg.AppendLine("Element id - element centroid - value at centroid:");
			foreach (int elementID in fieldAtCentroids.Keys)
			{
				msg.Append($"elem {elementID} - {centroidsOfElements[elementID]} - {fieldAtCentroids[elementID]}");
				numElementsAtCurrentLine++;
				if (numElementsAtCurrentLine == numElementsPerLine)
				{
					msg.AppendLine();
					numElementsAtCurrentLine = 0;
				}
				else
				{
					msg.Append(" | ");
				}
			}

			Console.WriteLine(msg);
		}
	}
}
