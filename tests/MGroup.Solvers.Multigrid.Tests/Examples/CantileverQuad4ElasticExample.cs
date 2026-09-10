namespace MGroup.Solvers.Multigrid.Tests.Examples
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Generation.Custom;

	public class CantileverQuad4ElasticExample
	{
		public double LengthX { get; set; } = 10.00;

		public double LengthY { get; set; } = 0.50;

		public double Thickness { get; set; } = 0.25;

		public double EndPointLoad { get; set; } = 20.0E3;

		public double YoungModulus { get; set; } = 2.1E7;

		public double PoissonRatio { get; set; } = 0.3;

		public Model CreateFemModel(int numElementsX, int numElementsY)
		{
			const int subdomainID = 0;

			// Material and section properties
			var material = new ElasticMaterial2D(YoungModulus, PoissonRatio, StressState2D.PlaneStress);
			var dynamicProperties = new TransientAnalysisProperties(1.0, 0.0, 0.0);

			// Model with 1 subdomain
			var model = new Model();
			model.SubdomainsDictionary.Add(subdomainID, new Subdomain(subdomainID));

			// Generate mesh
			var meshGenerator = new UniformMeshGenerator2D<Node>(0.0, 0.0, LengthX, LengthY,
				numElementsX, numElementsY);
			(var vertices, var cells) =
				meshGenerator.CreateMesh((id, x, y, z) => new Node(id: id, x: x, y: y, z: z));

			// Add nodes to the model
			for (var n = 0; n < vertices.Count; ++n) model.NodesDictionary.Add(n, vertices[n]);

			// Add Quad4 elements to the model
			var factory = new ContinuumElement2DFactory(Thickness, material, dynamicProperties);
			for (var e = 0; e < cells.Count; ++e)
			{
				var element = factory.CreateElement(cells[e].CellType, cells[e].Vertices);
				element.ID = e;
				model.ElementsDictionary.Add(e, element);
				model.SubdomainsDictionary[subdomainID].Elements.Add(element);
			}

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
			var loadPerNode = EndPointLoad / loadedNodes.Length;
			var neumannBCs = new List<INodalLoadBoundaryCondition>();
			foreach (var node in loadedNodes)
			{
				neumannBCs.Add(new NodalLoad(node, StructuralDof.TranslationY, loadPerNode));
			}

			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(dirichletBCs, neumannBCs));

			return model;
		}
	}
}
