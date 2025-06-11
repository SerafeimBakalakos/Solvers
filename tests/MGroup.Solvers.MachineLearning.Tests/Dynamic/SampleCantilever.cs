using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Statistics.Mcmc;
using MGroup.Constitutive.Structural.BoundaryConditions;

using MGroup.Constitutive.Structural;
using MGroup.Constitutive.Structural.Planar;
using MGroup.Constitutive.Structural.Transient;
using MGroup.FEM.Structural.Continuum;
using MGroup.MSolve.Discretization.Entities;
using MGroup.MSolve.Discretization.Meshes.Structured;
using MGroup.Solvers.MachineLearning.Tests.Utilities;
using MGroup.Solvers.Direct;
using MGroup.NumericalAnalyzers;
using MGroup.LinearAlgebra.Matrices;
using MGroup.LinearAlgebra.Vectors;
using MGroup.LinearAlgebra.Output;
using MGroup.LinearAlgebra.Output.Formatting;

namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{
    public static class SampleCantilever
    {
		public static void BuildLinearSystem()
		{
			double E = 200E6;
			double poisson = 0.3;
			double distributedLoad = 10;
			double thickness = 1;
			double lengthX = 20;
			double lengthY = 9;
			int numElementsX = 5;
			int numElementsY = 3;

			Model model = CreateModel(E, poisson, distributedLoad, thickness, lengthX, lengthY, numElementsX, numElementsY);

			var solverFactory = new DenseMatrixSolver.Factory();
			var algebraicModel = solverFactory.BuildAlgebraicModel(model);
			DenseMatrixSolver solver = solverFactory.BuildSolver(algebraicModel);
			solver.PreventFromOverwrittingSystemMatrices();

			// Linear static analysis
			var provider = new ProblemStructural(model, algebraicModel);
			var childAnalyzer = new LinearAnalyzer(algebraicModel, solver, provider);
			var parentAnalyzer = new StaticAnalyzer(algebraicModel, provider, childAnalyzer);
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			Matrix Kff = algebraicModel.LinearSystem.Matrix.SingleMatrix;
			Vector Ff = algebraicModel.LinearSystem.RhsVector.SingleVector;

			string pathKff = "C:\\Users\\Serafeim\\Desktop\\AISolve\\ddm\\sample_Kff.txt";
			var matrixWriter = new FullMatrixWriter();
			matrixWriter.WriteToFile(Kff, pathKff);

			string pathFf = "C:\\Users\\Serafeim\\Desktop\\AISolve\\ddm\\sample_Ff.txt";
			var vectorWriter = new FullVectorWriter();
			vectorWriter.NumericFormat = new GeneralNumericFormat();
			vectorWriter.WriteToFile(Ff, pathFf);
		}

		private static Model CreateModel(double elasticityModulus, double poissonRatio, double distributedLoad, double thickness,
			double lengthX, double lengthY, int numElementsX, int numElementsY)
		{
			// Mesh
			double[] minCoords = { 0.0, 0.0 };
			double[] maxCoords = { lengthX, lengthY };
			int[] numElementsPerAxis = { numElementsX, numElementsY };
			var meshBuilder = new UniformCartesianMesh2D.Builder(minCoords, maxCoords, numElementsPerAxis);
			meshBuilder.SetElementNodeOrderCounterClockwise();
			meshBuilder.SetMajorAxis(1);
			UniformCartesianMesh2D mesh = meshBuilder.BuildMesh();

			var model = new Model();
			model.SubdomainsDictionary.Add(0, new Subdomain(0));

			// Nodes
			for (var nodeID = 0; nodeID < mesh.NumNodesTotal; nodeID++)
			{
				double[] nodeCoords = mesh.GetNodeCoordinates(mesh.GetNodeIdx(nodeID));
				model.NodesDictionary[nodeID] = new Node(nodeID, nodeCoords[0], nodeCoords[1]);
			}

			// Elements
			var dynamicProperties = new TransientAnalysisProperties(density: 0.0, rayleighCoeffMass: 0.0, rayleighCoeffStiffness: 0.0);
			var elementFactory = new ContinuumElement2DFactory(thickness, null, dynamicProperties);
			for (int elementID = 0; elementID < mesh.NumElementsTotal; elementID++)
			{
				int[] nodeIds = mesh.GetElementConnectivity(mesh.GetElementIdx(elementID));
				INode[] nodesOfElement = nodeIds.Select(n => model.GetNode(n)).ToArray();

				var material = new ElasticMaterial2D(elasticityModulus, poissonRatio, StressState2D.PlaneStress);
				var element = elementFactory.CreateElement(mesh.CellType, nodesOfElement, thickness, material, dynamicProperties);
				element.ID = elementID;

				model.ElementsDictionary.Add(element.ID, element);
				model.SubdomainsDictionary[0].Elements.Add(element);
			}

			// Boundary conditions
			IEnumerable<INode> constrainedNodes = FindNodesWithX(0.0, model, maxCoords, numElementsPerAxis);
			var constraints = new List<INodalDisplacementBoundaryCondition>();
			foreach (INode node in constrainedNodes)
			{
				constraints.Add(new NodalDisplacement(node, StructuralDof.TranslationX, amount: 0.0));
				constraints.Add(new NodalDisplacement(node, StructuralDof.TranslationY, amount: 0.0));
			}

			var loads = new List<INodalLoadBoundaryCondition>();
			IEnumerable<INode> loadedNodes = FindNodesWithY(lengthY, model, maxCoords, numElementsPerAxis).Except(constrainedNodes);
			double loadPerNode = distributedLoad * lengthX / numElementsX;
			foreach (INode node in loadedNodes)
			{
				loads.Add(new NodalLoad(node, StructuralDof.TranslationY, amount: -loadPerNode));
			}

			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(constraints, loads));
			return model;
		}

		private static IEnumerable<INode> FindNodesWithX(double x, Model model, double[] domainDimensions, int[] numElementsPerAxis)
		{
			double dist = domainDimensions[0] / numElementsPerAxis[0];
			double tol = dist / 10;
			return model.NodesDictionary.Values.Where(node => Math.Abs(node.X - x) <= tol);
		}

		private static IEnumerable<INode> FindNodesWithY(double y, Model model, double[] domainDimensions, int[] numElementsPerAxis)
		{
			double dist = domainDimensions[1] / numElementsPerAxis[1];
			double tol = dist / 10;
			return model.NodesDictionary.Values.Where(node => Math.Abs(node.Y - y) <= tol);
		}
	}
}
