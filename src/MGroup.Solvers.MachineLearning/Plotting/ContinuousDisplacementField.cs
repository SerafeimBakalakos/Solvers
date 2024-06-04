namespace MGroup.Solvers.MachineLearning.Plotting
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using MGroup.MSolve.Discretization.Meshes.Output.VTK;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Solution.AlgebraicModel;

	using MGroup.MSolve.Solution.LinearSystem;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.Constitutive.Structural;

	using MGroup.MSolve.Discretization.BoundaryConditions;

	public class ContinuousDisplacementField
	{
		private const double offsetTol = 1E-6;

		private readonly Model model;
		private readonly IAlgebraicModel algebraicModel;
		private readonly ContinuousOutputMesh outMesh;
		private readonly IDofType[] dofs;

		public ContinuousDisplacementField(int dimension, Model model, IAlgebraicModel algebraicModel, 
			ContinuousOutputMesh outMesh)
		{
			this.model = model;
			this.algebraicModel = algebraicModel;
			this.outMesh = outMesh;
			if (dimension == 3)
			{
				dofs = new IDofType[3] { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ };
			}
			else if (dimension == 2) 
			{
				dofs = new IDofType[2] { StructuralDof.TranslationX, StructuralDof.TranslationY };
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public IReadOnlyList<double[]> CalcValuesAtVertices(IGlobalVector solution)
		{
			//var outDisplacements = new Dictionary<int, double[]>();
			var outDisplacements = new List<double[]>(model.NodesDictionary.Count);
			foreach (INode node in model.EnumerateNodes())
			{
				double[] nodalVector = algebraicModel.ExtractNodalValues(solution, node, dofs);
				//outDisplacements[node.ID] = nodalVector;
				outDisplacements.Add(nodalVector);
			}

			return outDisplacements;
		}
	}
}
