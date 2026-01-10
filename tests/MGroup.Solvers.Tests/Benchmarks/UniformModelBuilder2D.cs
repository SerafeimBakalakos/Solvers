//TODO: Allow option for prescribed displacement/load at corners or specific node index/ID.
namespace MGroup.Solvers.Tests.Commons
{
	using System.Data;
	using System.Diagnostics;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.Continuum;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.Solvers.Tests.DiscretizationExtensions;

	public class UniformModelBuilder2D
	{
		public enum BoundaryRegion
		{
			LeftSide, RightSide, UpperSide, LowerSide, UpperLeftCorner, UpperRightCorner, LowerLeftCorner, LowerRightCorner,
			Center
		}

		private List<(BoundaryRegion region, IStructuralDofType dof, double displacement)> prescribedDisplacements;
		private List<(BoundaryRegion region, IStructuralDofType dof, double load)> prescribedLoads;

		public UniformModelBuilder2D()
		{
			prescribedDisplacements = new List<(BoundaryRegion region, IStructuralDofType dof, double displacement)>();
			prescribedLoads = new List<(BoundaryRegion region, IStructuralDofType dof, double load)>();
		}

		public int AxisMajor { get; set; } = 0;

		public double[] CoordsMin { get; set; } = { -1.0, -1.0 };

		public double[] CoordsMax { get; set; } = { 1.0, 1.0 };

		public int[] NumElements { get; set; } = { 1, 1 };

		public double Thickness { get; set; } = 1.0;

		public IContinuumMaterial2D MaterialHomogeneous { get; set; }
			= new ElasticMaterial2D(1.0, 0.3, StressState2D.PlaneStress);

		public Func<int[], IContinuumMaterial2D> GetMaterialPerElementIndex { get; set; } = null;

		public Model BuildModel()
		{
			var model = new Model();
			model.SubdomainsDictionary[0] = new Subdomain(0);

			UniformCartesianMesh2D mesh = BuildMesh();

			// Nodes
			foreach ((int id, double[] coords) in mesh.EnumerateNodes())
			{
				model.NodesDictionary[id] = new Node(id, coords[0], coords[1]);
			}

			// Elements
			var dynamicProperties = new TransientAnalysisProperties(1.0, 1.0, 1.0);
			var elemFactory = new ContinuumElement2DFactory(Thickness, MaterialHomogeneous, dynamicProperties);
			foreach ((int elementID, int[] nodeIDs) in mesh.EnumerateElements())
			{
				// Identify which material to use
				IContinuumMaterial2D elementMaterial;
				if (GetMaterialPerElementIndex == null)
				{
					elementMaterial = MaterialHomogeneous;
				}
				else
				{
					int[] elementIdx = mesh.GetElementIdx(elementID);
					elementMaterial = GetMaterialPerElementIndex(elementIdx);
				}

				INode[] nodes = nodeIDs.Select(n => model.NodesDictionary[n]).ToArray();
				var element = elemFactory.CreateElement(mesh.CellType, nodes, Thickness, elementMaterial, dynamicProperties);
				element.ID = elementID;
				model.ElementsDictionary[element.ID] = element;
				model.SubdomainsDictionary[0].Elements.Add(element);
			}

			ApplyBoundaryConditions(model);

			return model;
		}

		/// <summary>
		/// If there are nodes belonging to <paramref name="region"/> taht are constrained along <paramref name="dof"/>, then 
		/// they will not be loaded, but the total <paramref name="load"/> will be divided by the total count of nodes, even the
		/// ones that will not be loaded.
		/// If there are multiple loads at the same (node, dof) then their sum will be used.
		/// </summary>
		/// <param name="load">Will be distributed evenly.</param>
		public void DistributeLoadAtNodes(BoundaryRegion region, IStructuralDofType dof, double load)
			=> prescribedLoads.Add((region, dof, load));

		public void PrescribeDisplacement(BoundaryRegion region, IStructuralDofType dof, double displacement)
			=> prescribedDisplacements.Add((region, dof, displacement));


		private void ApplyBoundaryConditions(Model model)
		{
			var bcBuilder = new IncrementalBCBuilderStructural(model);
			double dx = (CoordsMax[0] - CoordsMin[0]) / NumElements[0];
			double dy = (CoordsMax[1] - CoordsMin[1]) / NumElements[1];
			double meshTolerance = 1E-10 * Math.Min(dx, dy);

			// Apply prescribed Dirichlet BCs
			foreach ((BoundaryRegion region, IStructuralDofType dof, double displacement) in prescribedDisplacements)
			{
				INode[] nodes = FindBoundaryNodes(region, model, meshTolerance);
				foreach (INode node in nodes)
				{
					bcBuilder.AddDirichletBC(node, dof, displacement);
				}
			}

			// Apply prescribed loads
			foreach ((BoundaryRegion region, IStructuralDofType dof, double totalLoad) in prescribedLoads)
			{
				INode[] nodes = FindBoundaryNodes(region, model, meshTolerance);
				double load = totalLoad / nodes.Length;
				foreach (INode node in nodes)
				{
					bcBuilder.AddNeumannBC(node, dof, load);
				}
			}

			bcBuilder.ConfirmBoundaryConditions();
		}

		private UniformCartesianMesh2D BuildMesh()
		{
			if (AxisMajor != 0 && AxisMajor != 1)
			{
				throw new ArgumentException("Major axis must be 0 (x) or 1 (y)");
			}

			return new UniformCartesianMesh2D.Builder(CoordsMin, CoordsMax, NumElements)
				.SetMajorAxis(AxisMajor)
				.BuildMesh();
		}

		private INode[] FindBoundaryNodes(BoundaryRegion region, Model model, double tol)
		{
			double minX = CoordsMin[0], minY = CoordsMin[1], maxX = CoordsMax[0], maxY = CoordsMax[1]; // for brevity

			IEnumerable<INode> allNodes = model.EnumerateNodes();
			IEnumerable<INode> nodes;
			if (region == BoundaryRegion.LeftSide)
			{
				nodes = allNodes.Where(node => Math.Abs(node.X - minX) <= tol);
			}
			else if (region == BoundaryRegion.RightSide)
			{
				nodes = allNodes.Where(node => Math.Abs(node.X - maxX) <= tol);
			}
			else if (region == BoundaryRegion.LowerSide)
			{
				nodes = allNodes.Where(node => Math.Abs(node.Y - minY) <= tol);
			}
			else if (region == BoundaryRegion.UpperSide)
			{
				nodes = allNodes.Where(node => Math.Abs(node.Y - maxY) <= tol);
			}
			else if (region == BoundaryRegion.LowerLeftCorner)
			{
				nodes = allNodes.Where(node => (Math.Abs(node.X - minX) <= tol) && (Math.Abs(node.Y - minY) <= tol));
			}
			else if (region == BoundaryRegion.LowerRightCorner)
			{
				nodes = allNodes.Where(node => (Math.Abs(node.X - maxX) <= tol) && (Math.Abs(node.Y - minY) <= tol));
			}
			else if (region == BoundaryRegion.UpperLeftCorner)
			{
				nodes = allNodes.Where(node => (Math.Abs(node.X - minX) <= tol) && (Math.Abs(node.Y - maxY) <= tol));
			}
			else if (region == BoundaryRegion.UpperRightCorner)
			{
				nodes = allNodes.Where(node => (Math.Abs(node.X - maxX) <= tol) && (Math.Abs(node.Y - maxY) <= tol));
			}
			else if (region == BoundaryRegion.Center)
			{
				if ((NumElements[0] % 2 != 0) || (NumElements[1] % 2 != 0))
				{
					throw new ArgumentException(
						"To manipulate the node at the centre, the number of elements in each axis must be even");
				}

				double centerX = 0.5 * (CoordsMin[0] + CoordsMax[0]);
				double centerY = 0.5 * (CoordsMin[1] + CoordsMax[1]);
				nodes = allNodes.Where(node => (Math.Abs(node.X - centerX) <= tol) && (Math.Abs(node.Y - centerY) <= tol));
				Debug.Assert(nodes.Count() == 1);
			}
			else
			{
				throw new Exception("Should not have reached this code");
			}

			return nodes.ToArray();
		}
	}
}
