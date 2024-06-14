namespace MGroup.Solvers.MachineLearning.Tests.Dynamic
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using DotNumerics.ODE.Radau5;
	using DotNumerics.Optimization;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Continuum;
	using MGroup.Constitutive.Structural.InitialConditions;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Transient;
	using MGroup.FEM.Structural.Continuum;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Structured;
	using MGroup.Solvers.MachineLearning.Tests.StatisticsExtensions;

	using Tensorflow.Keras.Metrics;

	public class CantileverDynamicModel
	{
		private readonly bool use3DElements;
		private readonly int[] numElementsPerAxis;

		private CantileverDynamicModel(bool use3DElements, int[] numNodesPerAxis)
		{
			this.use3DElements = use3DElements;
			this.numElementsPerAxis = numNodesPerAxis;
		}

		public double BeamLength { get; set; } = 10.0;

		public double BeamSectionHeight { get; set; } = 2.0;

		public double BeamSectionWidth { get; set; } = 1.0;

		public double ElasticityModulusMean { get; set; } = 200E6;

		public double ElasticityModulusStdDev { get; set; } = 10E6;

		/// <summary>
		/// In kN
		/// </summary>
		public double ExternalLoadAmplitude { get; set; } = 20;

		/// <summary>
		/// In rad/s
		/// </summary>
		public double ExternalLoadCyclicFrequency { get; set; } = 15;

		public double PoissonRatio { get; set; } = 0.3;

		public Random Rng { get; set; } = new Random();

		public bool UseLogNormalDistribution { get; set; } = false;

		/// <summary>
		/// In seconds.
		/// </summary>
		public double TimeStep { get; private set; } = 0.1;

		/// <summary>
		/// In seconds
		/// </summary>
		public double TotalDuration { get; private set; } = 100;

		public void SetTime(double totalDuration, int numTimeSteps)
		{
			TotalDuration = totalDuration;
			TimeStep = totalDuration / numTimeSteps;
		}

		public static CantileverDynamicModel Create2DExample(int numElementsX, int numElementsY)
		{
			return new CantileverDynamicModel(false, new int[] { numElementsX, numElementsY });
		}

		public static CantileverDynamicModel Create3DExample(int numElementsX, int numElementsY, int numElementsZ)
		{
			return new CantileverDynamicModel(true, new int[] { numElementsX, numElementsY, numElementsZ });
		}

		public (Model model, double[] parameters, int monitorNodeId) CreateFemModel()
		{
			double[] elementElasticities = GenerateRandomElementElasticities();
			Model model = use3DElements ? CreateMesh3D(elementElasticities) : CreateMesh2D(elementElasticities);
			ApplyPermanentBoundaryConditions(model);
			ApplyInitialConditions(model);
			ApplyDynamicTopLoad(model);

			int monitorNodeId = FindMonitorNode(model);
			return (model, elementElasticities, monitorNodeId);
		}

		private Model CreateMesh2D(double[] elementElasticities)
		{
			// Mesh
			double[] minCoords = { 0.0, 0.0 };
			double[] maxCoords = { BeamSectionHeight, BeamLength };
			var meshBuilder = new UniformCartesianMesh2D.Builder(minCoords, maxCoords, numElementsPerAxis);
			meshBuilder.SetElementNodeOrderCounterClockwise();
			meshBuilder.SetMajorAxis(0);
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
			double thickness = BeamSectionWidth;
			var dynamicProperties = new TransientAnalysisProperties(density: 1.0, rayleighCoeffMass: 0.0, rayleighCoeffStiffness: 0.0);
			var elementFactory = new ContinuumElement2DFactory(BeamSectionWidth, null, dynamicProperties);
			for (int elementID = 0; elementID < mesh.NumElementsTotal; elementID++)
			{
				int[] nodeIds = mesh.GetElementConnectivity(mesh.GetElementIdx(elementID));
				INode[] nodesOfElement = nodeIds.Select(n => model.GetNode(n)).ToArray();

				var material = new ElasticMaterial2D(elementElasticities[elementID], PoissonRatio, StressState2D.PlaneStress);
				var element = elementFactory.CreateElement(CellType.Quad4, nodesOfElement, thickness, material, dynamicProperties);
				element.ID = elementID;

				model.ElementsDictionary.Add(element.ID, element);
				model.SubdomainsDictionary[0].Elements.Add(element);
			}

			return model;
		}

		private Model CreateMesh3D(double[] elementElasticities)
		{
			// Mesh
			double[] minCoords = { 0.0, 0.0, 0.0 };
			double[] maxCoords = { BeamSectionHeight, BeamSectionWidth, BeamLength };
			var meshBuilder = new UniformCartesianMesh3D.Builder(minCoords, maxCoords, numElementsPerAxis);
			meshBuilder.SetElementNodeOrderBathe();
			meshBuilder.SetMajorMinorAxis(0, 2);
			UniformCartesianMesh3D mesh = meshBuilder.BuildMesh();

			var model = new Model();
			model.SubdomainsDictionary.Add(0, new Subdomain(0));

			// Nodes
			for (var nodeID = 0; nodeID < mesh.NumNodesTotal; nodeID++)
			{
				double[] nodeCoords = mesh.GetNodeCoordinates(mesh.GetNodeIdx(nodeID));
				model.NodesDictionary[nodeID] = new Node(nodeID, nodeCoords[0], nodeCoords[1], nodeCoords[2]);
			}

			// Elements
			var dynamicProperties = new TransientAnalysisProperties(density: 1.0, rayleighCoeffMass: 0.0, rayleighCoeffStiffness: 0.0);
			var elementFactory = new ContinuumElement3DFactory(null, dynamicProperties);
			for (int elementID = 0; elementID < mesh.NumElementsTotal; elementID++)
			{
				int[] nodeIds = mesh.GetElementConnectivity(mesh.GetElementIdx(elementID));
				INode[] nodesOfElement = nodeIds.Select(n => model.GetNode(n)).ToArray();

				var material = new ElasticMaterial3D(elementElasticities[elementID], PoissonRatio);
				var element = elementFactory.CreateElement(CellType.Hexa8, nodesOfElement, material, dynamicProperties);
				element.ID = elementID;

				model.ElementsDictionary.Add(element.ID, element);
				model.SubdomainsDictionary[0].Elements.Add(element);
			}

			return model;
		}

		private void ApplyPermanentBoundaryConditions(Model model)
		{
			IEnumerable<INode> constrainedNodes = FindNodesAtSection(0.0, model);
			var permanentConstraints = new List<INodalDisplacementBoundaryCondition>();
			foreach (INode node in constrainedNodes)
			{
				permanentConstraints.Add(new NodalDisplacement(node, StructuralDof.TranslationX, amount: 0.0));
				permanentConstraints.Add(new NodalDisplacement(node, StructuralDof.TranslationY, amount: 0.0));
				if (use3DElements)
				{
					permanentConstraints.Add(new NodalDisplacement(node, StructuralDof.TranslationZ, amount: 0.0));
				}
			}

			var permanentLoads = new List<INodalLoadBoundaryCondition>(); // Empty: No permanent nodal loads
			model.BoundaryConditions.Add(new StructuralBoundaryConditionSet(permanentConstraints, permanentLoads));
		}

		private void ApplyInitialConditions(Model model)
		{
			var constrainedNodes = new HashSet<INode>(FindNodesAtSection(0.0, model));
			var domainInitialConditions = new List<IDomainStructuralInitialCondition>(); // Empty
			var initialDisplacements = new List<INodalDisplacementInitialCondition>();

			foreach (INode node in model.EnumerateNodes())
			{
				if (!constrainedNodes.Contains(node))
				{
					initialDisplacements.Add(new NodalInitialDisplacement(node, StructuralDof.TranslationX, amount: 0.0));
					initialDisplacements.Add(new NodalInitialDisplacement(node, StructuralDof.TranslationY, amount: 0.0));
					if (use3DElements)
					{
						initialDisplacements.Add(new NodalInitialDisplacement(node, StructuralDof.TranslationZ, amount: 0.0));
					}
				}
			}

			model.InitialConditions.Add(new StructuralInitialConditionSet(initialDisplacements, domainInitialConditions));
		}

		private void ApplyDynamicTopLoad(Model model)
		{
			IEnumerable<INode> loadedNodes = FindNodesAtSection(BeamLength, model);
			var spatialLoadDistribution = new List<INodalLoadBoundaryCondition>();
			foreach (INode node in loadedNodes)
			{
				spatialLoadDistribution.Add(new NodalLoad(node, StructuralDof.TranslationX, amount: 1.0));
			}

			var load = new DynamicLoad(ExternalLoadAmplitude, ExternalLoadCyclicFrequency, TimeStep);
			var transientConstraints = new List<INodalDisplacementBoundaryCondition>(); // Empty: no transient constraints
			var transientBoundaryConditions = new StructuralTransientBoundaryConditionSet(
				new List<IBoundaryConditionSet<IStructuralDofType>>()
				{
					new StructuralBoundaryConditionSet(transientConstraints, spatialLoadDistribution)
				},
				load.Evaluate/*EvaluateExternalLoad*/);
			model.BoundaryConditions.Add(transientBoundaryConditions);
		}

		private void ApplyGroundMotion(Model model)
		{
			throw new NotImplementedException();
		}

		private int CountElements()
		{
			if (use3DElements)
			{
				return numElementsPerAxis[0] * numElementsPerAxis[1] * numElementsPerAxis[2];
			}
			else
			{
				return numElementsPerAxis[0] * numElementsPerAxis[1];
			}
		}

		private double EvaluateExternalLoad(double t, double spatialLoadComponent)
		{
			Console.WriteLine(t);
			return spatialLoadComponent * ExternalLoadAmplitude * Math.Sin(ExternalLoadCyclicFrequency * t);
		}

		private IEnumerable<INode> FindNodesAtSection(double distanceOnAxis, Model model)
		{
			double dist = BeamLength / numElementsPerAxis[numElementsPerAxis.Length - 1];
			double tol = dist / 10;
			return use3DElements ?
				model.NodesDictionary.Values.Where(node => Math.Abs(node.Z - distanceOnAxis) <= tol) :
				model.NodesDictionary.Values.Where(node => Math.Abs(node.Y - distanceOnAxis) <= tol);
		}

		private int FindMonitorNode(Model model)
		{
			double distHeight = BeamSectionHeight / numElementsPerAxis[0];
			double distLength = BeamLength / numElementsPerAxis[numElementsPerAxis.Length - 1];
			INode monitorNode;
			if (use3DElements)
			{
				double distWidth = BeamSectionWidth / numElementsPerAxis[1];
				monitorNode = model.NodesDictionary.Values
					.Where(node => Math.Abs(node.Z - BeamLength) <= distLength / 10)
					.Where(node => Math.Abs(node.X - BeamSectionHeight) <= distHeight / 10)
					.Where(node => Math.Abs(node.Y - BeamSectionWidth) <= distWidth / 10)
					.FirstOrDefault();
			}
			else
			{
				monitorNode = monitorNode = model.NodesDictionary.Values
					.Where(node => Math.Abs(node.Y - BeamLength) <= distLength / 10)
					.Where(node => Math.Abs(node.X - BeamSectionHeight) <= distHeight / 10)
					.FirstOrDefault();
			}

			if (monitorNode != null)
			{
				return monitorNode.ID;
			}
			else
			{
				throw new Exception("No monitor node found. This is a bug.");
			}
		}

		private double[] GenerateRandomElementElasticities()
		{
			IDistribution distribution = UseLogNormalDistribution 
				? LogNormalDistribution.CreateWithMeanStddev(Rng, ElasticityModulusMean, ElasticityModulusStdDev)
				: NormalDistribution.CreateWithMeanStddev(Rng, ElasticityModulusMean, ElasticityModulusStdDev);

			int numSamples = CountElements();
			double[] samples = distribution.GenerateSamples(numSamples);

			#region debug
			//Console.WriteLine("Elasticities: ");
			//for (int i = 0; i < samples.Length; i++)
			//{
			//	Console.WriteLine($"{samples[i]}");
			//}
			//Console.WriteLine();
			#endregion
			return samples;
		}

		private class DynamicLoad
		{
			private readonly double amplitude;
			private readonly double frequency;
			private readonly double delay;

			public DynamicLoad(double amplitude, double frequency, double delay)
			{
				this.amplitude = amplitude;
				this.frequency = frequency;
				this.delay = delay;
			}

			public double Evaluate(double t, double spatialLoadComponent)
			{
				double time = t;
				//double time = t + delay;
				//Console.WriteLine(time);
				return spatialLoadComponent * amplitude * Math.Sin(frequency * time);
			}
		}
	}
}
