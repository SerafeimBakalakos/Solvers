namespace MGroup.Solvers.Tests.MatrixFree
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.Constitutive.Structural;
	using MGroup.Constitutive.Structural.BoundaryConditions;
	using MGroup.Constitutive.Structural.Planar;
	using MGroup.Constitutive.Structural.Providers;
	using MGroup.Environments;
	using MGroup.LinearAlgebra.Distributed.Overlapping;
	using MGroup.LinearAlgebra.Matrices;
	using MGroup.LinearAlgebra.Vectors;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.DiscretizationExtensions;
	using MGroup.Solvers.DofOrdering_v2;
	using MGroup.Solvers.LinearSystem;
	using MGroup.Solvers.MatrixFree;
	using MGroup.Solvers.MatrixFree.Dofs;
	using MGroup.Solvers.MatrixFree.ElementMatrices;
	using MGroup.Solvers.Tests.Commons;

	using Xunit;

	public static class DofScalingTests
	{
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public static void TestHeterogeneousDofScaling(bool isMaterialHomogeneous)
		{
			IModel_v2 model = CreateModel(isMaterialHomogeneous);
			IComputeEnvironment environment = CreateEnvironment(model);
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			var domain = new FullDomain_v2(model, elementMatrixProvider, cacheElementDofs: true);
			var partition = new DefaultElementPartition(environment, model, domain);
			var dofManager = new DistributedDofManager(environment, domain, partition);
			var linearSystem = new LinearSystem_v2();
			var dofScaling = new HeterogeneousDofScaling(environment, domain, linearSystem);

			partition.FindElementNeighbors();
			dofManager.PrepareDofs();
			var distributedMatrix = new DistributedOverlappingMatrix<IMatrix>(dofManager.DistributedIndexer);
			environment.DoPerNode(elementID =>
			{
				ISuperElement element = domain.GetElement(elementID);
				IMatrix elementMatrix = element.BuildMatrix();
				distributedMatrix.LocalMatrices[elementID] = elementMatrix;
			});
			linearSystem.Matrix = distributedMatrix;

			dofScaling.Update();

			if (isMaterialHomogeneous)
			{
				Dictionary<int, double[]> expectedMultiplicities = GetElementDofMultiplicities();
				CheckArrays(expectedMultiplicities, dofScaling, 1E-15);
			}
			else
			{
				Dictionary<int, double[]> expectedRelativeStiffnesses = GetElementRelativeStiffnesses();
				CheckArrays(expectedRelativeStiffnesses, dofScaling, 1E-15);
			}
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public static void TestHomogeneousDofScaling(bool isMaterialHomogeneous)
		{
			IModel_v2 model = CreateModel(isMaterialHomogeneous);
			IComputeEnvironment environment = CreateEnvironment(model);
			var elementMatrixProvider = new ElementStructuralStiffnessProvider();
			var domain = new FullDomain_v2(model, elementMatrixProvider, cacheElementDofs: true);
			var partition = new DefaultElementPartition(environment, model, domain);
			var dofManager = new DistributedDofManager(environment, domain, partition);
			var dofScaling = new HomogeneousDofScaling(environment, domain, partition, dofManager);

			partition.FindElementNeighbors();
			dofManager.PrepareDofs();
			var distributedMatrix = new DistributedOverlappingMatrix<IMatrix>(dofManager.DistributedIndexer);
			dofScaling.Update();

			Dictionary<int, double[]> expectedMultiplicities = GetElementDofMultiplicities();
			CheckArrays(expectedMultiplicities, dofScaling, 1E-15);
		}

		private static void CheckArrays(Dictionary<int, double[]> expectedScalingDiagonals, IDofScaling dofScaling, double tolerance)
		{
			foreach (int e in expectedScalingDiagonals.Keys)
			{
				DiagonalMatrix scalingMatrix = dofScaling.GetScalingMatrix(e);
				var computed = Vector.CreateFromArray(scalingMatrix.RawDiagonal);
				var expected = Vector.CreateFromArray(expectedScalingDiagonals[e]);
				Assert.True(expected.Equals(computed, tolerance));
			}
		}

		private static IComputeEnvironment CreateEnvironment(IModel_v2 model)
		{
			var environment = new SequentialSharedEnvironment();
			var nodeTopology = new ComputeNodeTopology();
			foreach (IElementType element in model.EnumerateElements())
			{
				var neighbors = new HashSet<int>();
				foreach (INode node in element.Nodes)
				{
					neighbors.UnionWith(node.ElementsDictionary.Values.Select(e => e.ID));
				}
				neighbors.Remove(element.ID);

				nodeTopology.AddNode(element.ID, neighbors.ToArray(), 0);
			}

			environment.Initialize(nodeTopology);
			return environment;
		}

		private static IModel_v2 CreateModel(bool isMaterialHomogeneous)
		{
			var builder = new UniformModelBuilder2D();
			builder.CoordsMin = [0.0, 0.0];
			builder.CoordsMax = [4.0, 3.0];
			builder.NumElements = [4, 3];
			builder.Thickness = 0.01;
			builder.PrescribeDisplacement(UniformModelBuilder2D.BoundaryRegion.LowerLeftCorner, StructuralDof.TranslationX, 0);
			builder.PrescribeDisplacement(UniformModelBuilder2D.BoundaryRegion.LowerLeftCorner, StructuralDof.TranslationY, 0);
			builder.PrescribeDisplacement(UniformModelBuilder2D.BoundaryRegion.LowerRightCorner, StructuralDof.TranslationY, 0);
			builder.DistributeLoadAtNodes(UniformModelBuilder2D.BoundaryRegion.UpperSide, StructuralDof.TranslationY, -3E5);

			if (isMaterialHomogeneous)
			{
				double elasticity = 1E7;
				builder.MaterialHomogeneous = new ElasticMaterial2D(elasticity, 0.3, StressState2D.PlaneStress);
			}
			else
			{
				builder.GetMaterialPerElementIndex = (elementIdx) =>
				{
					double elasticity = (elementIdx[0] < builder.NumElements[0] / 2) ? 2E7 : 1E7;
					return new ElasticMaterial2D(elasticity, 0.3, StressState2D.PlaneStress);
				};
			}

			IModel_v2 model = new ModelAdapter_temp(builder.BuildModel());
			model.ConnectDataStructures();
			return model;
		}

		private static Dictionary<int, double[]> GetElementDofMultiplicities()
		{
			var multiplicities = new Dictionary<int, double[]>();

			// Row 0
			multiplicities[0] = [0.5, 0.5, 0.25, 0.25, 0.5, 0.5];
			multiplicities[1] = [0.5, 0.5, 0.5, 0.5, 0.25, 0.25, 0.25, 0.25];
			multiplicities[2] = [0.5, 0.5, 0.5, 0.5, 0.25, 0.25, 0.25, 0.25];
			multiplicities[3] = [0.5, 0.5, 1, 0.5, 0.5, 0.25, 0.25];

			// Row 1
			multiplicities[4] = [0.5, 0.5, 0.25, 0.25, 0.25, 0.25, 0.5, 0.5];
			multiplicities[5] = [0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25];
			multiplicities[6] = [0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25, 0.25];
			multiplicities[7] = [0.25, 0.25, 0.5, 0.5, 0.5, 0.5, 0.25, 0.25];

			// Row 2
			multiplicities[8] =  [0.5, 0.5, 0.25, 0.25, 0.5, 0.5, 1, 1];
			multiplicities[9] =  [0.25, 0.25, 0.25, 0.25, 0.5, 0.5, 0.5, 0.5];
			multiplicities[10] = [0.25, 0.25, 0.25, 0.25, 0.5, 0.5, 0.5, 0.5];
			multiplicities[11] = [0.25, 0.25, 0.5, 0.5, 1, 1, 0.5, 0.5];

			return multiplicities;
		}

		private static Dictionary<int, double[]> GetElementRelativeStiffnesses()
		{
			var multiplicities = new Dictionary<int, double[]>();

			// Row 0
			multiplicities[0] = [0.5, 0.5, 0.25, 0.25, 0.5, 0.5];
			multiplicities[1] = [0.5, 0.5, 2 / 3.0, 2 / 3.0, 2 / 6.0, 2 / 6.0, 0.25, 0.25];
			multiplicities[2] = [1 / 3.0, 1 / 3.0, 0.5, 0.5, 0.25, 0.25, 1 / 6.0, 1 / 6.0];
			multiplicities[3] = [0.5, 0.5, 1, 0.5, 0.5, 0.25, 0.25];

			// Row 1
			multiplicities[4] = [0.5, 0.5, 0.25, 0.25, 0.25, 0.25, 0.5, 0.5];
			multiplicities[5] = [0.25, 0.25, 2 / 6.0, 2 / 6.0, 2 / 6.0, 2 / 6.0, 0.25, 0.25];
			multiplicities[6] = [1 / 6.0, 1 / 6.0, 0.25, 0.25, 0.25, 0.25, 1 / 6.0, 1 / 6.0];
			multiplicities[7] = [0.25, 0.25, 0.5, 0.5, 0.5, 0.5, 0.25, 0.25];

			// Row 2
			multiplicities[8] = [0.5, 0.5, 0.25, 0.25, 0.5, 0.5, 1, 1];
			multiplicities[9] = [0.25, 0.25, 2 / 6.0, 2 / 6.0, 2 / 3.0, 2 / 3.0, 0.5, 0.5];
			multiplicities[10] = [1 / 6.0, 1 / 6.0, 0.25, 0.25, 0.5, 0.5, 1 / 3.0, 1 / 3.0];
			multiplicities[11] = [0.25, 0.25, 0.5, 0.5, 1, 1, 0.5, 0.5];

			return multiplicities;
		}
	}
}
