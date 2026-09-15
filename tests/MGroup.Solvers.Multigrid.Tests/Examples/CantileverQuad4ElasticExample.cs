namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Generation.Custom;
	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.Solvers.Multigrid.GridDefinition;

	public class CantileverQuad4ElasticExample
	{
		public double LengthX { get; set; } = 10.00;

		public double LengthY { get; set; } = 0.50;

		public double Thickness { get; set; } = 0.25;

		public double YoungModulus { get; set; } = 2.1E7;

		public double PoissonRatio { get; set; } = 0.3;

		public double EndPointLoad { get; set; } = 20.0E3;

		public bool ParallelToX { get; set; } = true;

		public Model CreateFemModel(Grid2D grid)
		{
			const int subdomainID = 0;

			// Material and section properties
			var material = new ElasticMaterial2D(YoungModulus, PoissonRatio, StressState2D.PlaneStress);
			var dynamicProperties = new TransientAnalysisProperties(1.0, 0.0, 0.0);

			// Model with 1 subdomain
			var model = new Model();
			model.SubdomainsDictionary.Add(subdomainID, new Subdomain(subdomainID));

			// Generate mesh
			UniformCartesianMesh2D mesh = grid.CreateMesh([0, 0], [LengthX, LengthY]);

			// Add nodes to the model
			foreach ((int nodeID, double[] coords) in mesh.EnumerateNodes())
			{
				model.NodesDictionary[nodeID] = new Node(nodeID, coords[0], coords[1]);
			}

			// Add Quad4 elements to the model
			var factory = new ContinuumElement2DFactory(Thickness, material, dynamicProperties);
			foreach ((int elementID, int[] nodeIDs) in mesh.EnumerateElements())
			{
				// Gather nodes
				int numNodes = nodeIDs.Length;
				var elementNodes = new INode[numNodes];
				for (int n = 0; n < numNodes; n++)
				{
					elementNodes[n] = model.NodesDictionary[nodeIDs[n]];
				}

				CellType cellType = mesh.CellType;
				ContinuumElement2D element = factory.CreateElement(cellType, elementNodes);
				element.ID = elementID;
				model.ElementsDictionary.Add(elementID, element);
				model.SubdomainsDictionary[subdomainID].Elements.Add(element);
			}

			if (ParallelToX) ApplyBCsAlongX(model);
			else ApplyBCsAlongY(model);

			return model;
		}

		private void ApplyBCsAlongX(Model model)
		{
			// Clamp boundary condition at one end
			var tol = 1E-10; //TODO: this should be chosen w.r.t. the element size along X
			var dirichletBCs = new List<INodalDisplacementBoundaryCondition>();
			foreach (var node in model.NodesDictionary.Values.Where(node => Math.Abs(node.X) <= tol))
			{
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationX, 0));
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationY, 0));
			}

			// Apply concentrated load at the other end
			var loadedNodes = model.NodesDictionary.Values.Where(node => Math.Abs(node.X - LengthX) <= tol).ToArray();
			var loadPerNode = - EndPointLoad / loadedNodes.Length;
			var neumannBCs = new List<INodalLoadBoundaryCondition>();
			foreach (var node in loadedNodes)
			{
				neumannBCs.Add(new NodalLoad(node, StructuralDof.TranslationY, loadPerNode));
			}

			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(dirichletBCs, neumannBCs));
		}

		private void ApplyBCsAlongY(Model model)
		{
			// Clamp boundary condition at one end
			var tol = 1E-10; //TODO: this should be chosen w.r.t. the element size along X
			var dirichletBCs = new List<INodalDisplacementBoundaryCondition>();
			foreach (var node in model.NodesDictionary.Values.Where(node => Math.Abs(node.Y) <= tol))
			{
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationX, 0));
				dirichletBCs.Add(new NodalDisplacement(node, StructuralDof.TranslationY, 0));
			}

			// Apply concentrated load at the other end
			var loadedNodes = model.NodesDictionary.Values.Where(node => Math.Abs(node.Y - LengthY) <= tol).ToArray();
			var loadPerNode = EndPointLoad / loadedNodes.Length;
			var neumannBCs = new List<INodalLoadBoundaryCondition>();
			foreach (var node in loadedNodes)
			{
				neumannBCs.Add(new NodalLoad(node, StructuralDof.TranslationX, loadPerNode));
			}

			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(dirichletBCs, neumannBCs));
		}
	}
}
