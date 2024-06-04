namespace MGroup.Solvers.MachineLearning.Plotting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Xml.Linq;

	using MGroup.MSolve.Discretization;
	using MGroup.MSolve.Discretization.Entities;
	using MGroup.MSolve.Discretization.Meshes.Output.VTK;

	public class ContinuousOutputMesh : IVtkMesh
	{
		private readonly SortedDictionary<int, VtkCell> outCells;
		private readonly SortedDictionary<int, VtkPoint> outVertices;

		public ContinuousOutputMesh(Model model) 
			: this(model.EnumerateNodes(), model.EnumerateElements(model.SubdomainsDictionary.Keys.First()))
		{ 
		} 

		public ContinuousOutputMesh(IEnumerable<INode> originalVertices, IEnumerable<IElementType> originalCells)
		{
			this.OriginalVertices = originalVertices;
			this.OriginalCells = originalCells;


			this.outVertices = new SortedDictionary<int, VtkPoint>();
			foreach (Node vertex in originalVertices)
			{
				var outVertex = new VtkPoint(vertex.ID, vertex);
				outVertices[vertex.ID] = outVertex;
			}

			this.outCells = new SortedDictionary<int, VtkCell>();
			foreach (IElementType cell in originalCells)
			{
				List<VtkPoint> vertices = cell.Nodes.Select(v => outVertices[v.ID]).ToList();
				outCells[cell.ID] = new VtkCell(cell.CellType, vertices);
			}
		}

		public int NumOutCells => outCells.Count;

		public int NumOutVertices => outVertices.Count;

		public IEnumerable<IElementType> OriginalCells { get; }

		/// <summary>
		/// Same order as the corresponding one in <see cref="OutVertices"/>.
		/// </summary>
		public IEnumerable<INode> OriginalVertices { get; }

		public IReadOnlyList<VtkCell> VtkCells => outCells.Values.ToList();

		/// <summary>
		/// Same order as the corresponding one in <see cref="OriginalVertices"/>.
		/// </summary>
		public IReadOnlyList<VtkPoint> VtkPoints => outVertices.Values.ToList();
	}
}
