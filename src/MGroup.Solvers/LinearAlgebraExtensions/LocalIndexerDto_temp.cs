//namespace MGroup.Solvers.LinearAlgebraExtensions
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Text;

//	using MGroup.Environments;

//	[Serializable]
//	public class LocalIndexerDto_temp
//	{
//		internal LocalIndexerDto_temp() { }
//		public static LocalIndexerDto_temp CreateUnmodified() => new LocalIndexerDto_temp { Modified = false };

//		public static LocalIndexerDto_temp CreateWithNewContent(
//			int numIndices, Dictionary<int, int[]> commonEntriesOfNodeWithNeighbors)
//		{
//			var dto = new LocalIndexerDto_temp
//			{
//				NumIndices = numIndices,
//				CommonEntriesOfNodeWithNeighbors = commonEntriesOfNodeWithNeighbors
//			};

//			return dto;
//		}

//		internal static LocalIndexerDto_temp CreateForSerialization(LocalIndexer_temp localIndexer)
//		{
//			var dto = new LocalIndexerDto_temp
//			{
//				NodeID = localIndexer.Node.ID,
//				NumIndices = localIndexer.NumIndices,
//				CommonEntriesOfNodeWithNeighbors = new Dictionary<int, int[]>()
//			};

//			foreach (int neighborID in localIndexer.ActiveNeighborsOfNode)
//			{
//				dto.CommonEntriesOfNodeWithNeighbors[neighborID] = localIndexer.GetCommonEntriesWithNeighbor(neighborID);
//			}

//			return dto;
//		}

//		public Dictionary<int, int[]> CommonEntriesOfNodeWithNeighbors { get; set; }

//		public bool Modified { get; set; } = true;

//		public int NodeID { get; set; } = -1;

//		public int NumIndices { get; set; }

//		internal LocalIndexer_temp ToLocalIndexer(IComputeEnvironment environment)
//		{
//			ComputeNode node = environment.GetComputeNode(this.NodeID);
//			return new LocalIndexer_temp(node, this.CommonEntriesOfNodeWithNeighbors, this.NumIndices);
//		}
//	}
//}
