using MGroup.Solvers.DiscretizationExtensions;

namespace MGroup.Solvers.DiscretizationExtensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.BoundaryConditions;
	using MGroup.MSolve.Discretization.Dofs;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Providers;
	using MGroup.Solvers.DiscretizationExtensions;

	public class FullDomain_temp : ISubdomain_v2
	{
		private readonly IModel_v2 model;
		private readonly IElementMatrixProvider elementMatrixProvider;

		public FullDomain_temp(IModel_v2 model, IElementMatrixProvider elementMatrixProvider)
		{
			this.model = model;
			this.elementMatrixProvider = elementMatrixProvider;
			this.ID = 0;
		}

		public int ID { get; }

		public IEnumerable<INode> EnumerateNodes() => model.EnumerateNodes();

		public IEnumerable<ISuperElement> EnumerateElements()
			=> model.EnumerateElements().Select(e => new DefaultElement_temp(e, model.DofTypes, elementMatrixProvider));

		public IntDofTable OrderDofs() 
			=> OrderFreeDofs(model.DofTypes, model.EnumerateElements(), model.EnumerateNodes(), model.GetDirichletBCs());

		public static IntDofTable OrderFreeDofs(ActiveDofs activeDofs, IEnumerable<IElementType> elements, IEnumerable<INode> nodes, IEnumerable<INodalDirichletBoundaryCondition<IDofType>> nodalDirichletBCs)
		{
			// Find all dofs
			var nodalDofTypesDictionary = new Dictionary<int, List<IDofType>>(); //TODO: use Set instead of List or a dedicated structure
			foreach (IElementType element in elements)
			{
				for (int i = 0; i < element.Nodes.Count; i++)
				{
					if (!nodalDofTypesDictionary.ContainsKey(element.Nodes[i].ID))
					{
						nodalDofTypesDictionary.Add(element.Nodes[i].ID, new List<IDofType>());
					}

					nodalDofTypesDictionary[element.Nodes[i].ID].AddRange(element.DofEnumerator.GetDofTypesForDofEnumeration(element)[i]);
				}
			}

			// Find constrained dofs
			var constrainedDofs = new HashDofSet<INode, IDofType>();
			foreach (INodalBoundaryCondition<IDofType> nodalBC in nodalDirichletBCs)
			{
				constrainedDofs.AddDof(nodalBC.Node, nodalBC.DOF);
			}

			// Order free dofs
			int dofIdx = 0;
			var freeDofs = new IntDofTable();
			foreach (INode node in nodes)
			{
				foreach (IDofType dofType in nodalDofTypesDictionary[node.ID].Distinct())
				{
					if (constrainedDofs.Contains(node, dofType) == false)
					{
						freeDofs[node.ID, activeDofs.GetIdOfDof(dofType)] = dofIdx++;
					}
				}
			}

			return freeDofs;
		}
	}
}
