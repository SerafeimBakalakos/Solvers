namespace MGroup.Solvers.DDM.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using MGroup.MSolve.AnalysisWorkflow.Transient;
	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;

	public class ModelAdapter_temp : IModel_v2
	{
		private IModel model;

		public ModelAdapter_temp(IModel model)
		{
			this.model = model;
		}

		public ActiveDofs DofTypes { get; private set; }

		public int NumSubdomains => model.NumSubdomains;

		public void ConnectDataStructures()
		{
			model.ConnectDataStructures();
			IdentifyUniqueDofTypes();
		}

		public IEnumerable<IBoundaryConditionSet<IDofType>> EnumerateBoundaryConditions(int subdomainID) => model.EnumerateBoundaryConditions(subdomainID);

		public IEnumerable<IElementType> EnumerateElements(int subdomainID) => model.EnumerateElements(subdomainID);

		public IEnumerable<IInitialConditionSet<IDofType>> EnumerateInitialConditions(int subdomainID) => model.EnumerateInitialConditions(subdomainID);

		public IEnumerable<INode> EnumerateNodes() => model.EnumerateNodes();

		public IEnumerable<ISubdomain> EnumerateSubdomains() => model.EnumerateSubdomains();

		public IEnumerable<INodalDirichletBoundaryCondition<IDofType>> FindDirichletBCsOfSubdomain(int subdomainID)
		{
			IEnumerable<IElementType> elements = model.EnumerateElements(subdomainID);
			var dirichletBCs = model.EnumerateBoundaryConditions(subdomainID)
				.SelectMany(x => x.EnumerateNodalBoundaryConditions(elements))
				.OfType<INodalDirichletBoundaryCondition<IDofType>>();
			return dirichletBCs;
		}

		public IEnumerable<INodalNeumannBoundaryCondition<IDofType>> FindNeumannBCsOfSubdomain(int subdomainID)
		{
			var elements = model.EnumerateElements(subdomainID);
			return model.EnumerateBoundaryConditions(subdomainID)
				.SelectMany(bcSet => bcSet.EnumerateNodalBoundaryConditions(elements))
				.OfType<INodalNeumannBoundaryCondition<IDofType>>();
		}

		public INode GetNode(int nodeID) => model.GetNode(nodeID);

		public ISubdomain GetSubdomain(int subdomainID) => model.GetSubdomain(subdomainID);

		private void IdentifyUniqueDofTypes()
		{
			DofTypes = new ActiveDofs();
			foreach (ISubdomain subdomain in EnumerateSubdomains())
			{
				foreach (IElementType element in subdomain.EnumerateElements())
				{
					IReadOnlyList<IReadOnlyList<IDofType>> elementDofs = element.DofEnumerator.GetDofTypesForDofEnumeration(element);
					foreach (IReadOnlyList<IDofType> dofsOfNode in elementDofs)
					{
						foreach (IDofType dofType in dofsOfNode)
						{
							DofTypes.AddDof(dofType);
						}
					}
				}
			}
		}
	}
}
