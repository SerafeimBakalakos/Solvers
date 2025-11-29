namespace MGroup.Solvers
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	public class HashDofSet<TNode, TDof>
	{
		private Dictionary<TNode, HashSet<TDof>> data = new Dictionary<TNode, HashSet<TDof>>();

		public void AddDof(TNode node, TDof dof)
		{
			bool nodeExists = data.TryGetValue(node, out HashSet<TDof> dofsOfThisNode);
			if (!nodeExists)
			{
				dofsOfThisNode = new HashSet<TDof>();
				data[node] = dofsOfThisNode;
			}
			dofsOfThisNode.Add(dof);
		}

		public void AddDofs(TNode node, IEnumerable<TDof> dofs)
		{
			bool nodeExists = data.TryGetValue(node, out HashSet<TDof> dofsOfThisNode);
			if (!nodeExists)
			{
				dofsOfThisNode = new HashSet<TDof>();
				data[node] = dofsOfThisNode;
			}
			dofsOfThisNode.UnionWith(dofs);
		}

		public bool Contains(TNode node, TDof dof)
		{
			bool nodeExists = data.TryGetValue(node, out HashSet<TDof> dofsOfThisNode);
			if (nodeExists)
			{
				return dofsOfThisNode.Contains(dof);
			}
			else
			{
				return false;
			}
		}

		public int Count()
		{
			int numEntries = 0;
			foreach (var nodeDofsPair in data)
			{
				numEntries += nodeDofsPair.Value.Count;
			}
			return numEntries;
		}
	}
}
