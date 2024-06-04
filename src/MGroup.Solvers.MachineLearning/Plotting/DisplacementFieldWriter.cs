namespace MGroup.Solvers.MachineLearning.Plotting
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Output.VTK;
	using MGroup.MSolve.Solution.AlgebraicModel;

	using MGroup.MSolve.Solution.LinearSystem;

	public class DisplacementFieldWriter
	{
		private readonly Model model;
		private readonly string outputDirectory;
		private readonly int dimension;
		private int iteration;

		public DisplacementFieldWriter(int dimension, Model model, string outputDirectory)
		{
			iteration = 0;
			this.model = model;
			this.outputDirectory = outputDirectory; // delete trailing separator characters
			this.dimension = dimension;
		}

		public void WriteResults(IAlgebraicModel algebraicModel, IGlobalVector solution)
		{
			var outputMesh = new ContinuousOutputMesh(model);
			var displacementField = new ContinuousDisplacementField(dimension, model, algebraicModel, outputMesh);
			//Dictionary<int, double[]> nodalDisplacements = displacementField.CalcValuesAtVertices(solution);
			IReadOnlyList<double[]> nodalDisplacements = displacementField.CalcValuesAtVertices(solution);
			var path = Path.Combine(outputDirectory, $"displacements_t{iteration}.vtk");
			using (var writer = new VtkFileWriter(path, dimension))
			{
				writer.WriteMesh(outputMesh);
				writer.WriteVectorField("displacements", nodalDisplacements);
			}

			++iteration;
		}
	}
}
