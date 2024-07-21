namespace MGroup.Solvers.MachineLearning.PodAmg.Surrogates
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using MGroup.MachineLearning.TensorFlow.KerasLayers;

	using MGroup.MachineLearning.Utilities;
	using MGroup.Solvers.MachineLearning.MLExtensions.TensorFlow;
	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptOut)]
	public class CaeFfnnArchitecture
	{
		public CaeFfnnArchitecture() 
		{
		}

		public int NumDofs { get; set; } = -1;

		public int NumModelParams { get; set; } = -1;

		public int LatentSpaceDim { get; set; } = -1;

		public int CaeBatchSize { get; set; } = -1;

		public int CaeNumEpochs { get; set; } = -1;

		public float CaeLearningRate { get; set; } = 0.0f;

		public int FfnnBatchSize { get; set; } = -1;

		public int FfnnNumEpochs { get; set; } = -1;

		public float FfnnLearningRate { get; set; } = 0.0f;

		public List<IKerasLayer> EncoderLayers { get; set; } = new List<IKerasLayer>();

		public List<IKerasLayer> DecoderLayers { get; set; } = new List<IKerasLayer>();

		public List<IKerasLayer> FfnnLayers { get; set; } = new List<IKerasLayer>();
	}
}
